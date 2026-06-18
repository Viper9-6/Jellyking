using Jellyking.Core.Models;

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Jellyking.Core.Services;

/// <summary>
/// Persists per-service credentials in data/credentials.json, encrypted with
/// ASP.NET Core Data Protection so the file is opaque at rest. Broadcasts a
/// Changed event whenever a credential changes so the proxy/session layer
/// can reload.
/// </summary>
public sealed class JsonCredentialStore : ICredentialStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IDataProtector _protector;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // In-memory: service id -> credential. Persisted encrypted on disk.
    private Dictionary<Guid, AuthCredential> _creds = new();

    public JsonCredentialStore(string dataDirectory, IDataProtectionProvider dp)
    {
        _filePath = Path.Combine(dataDirectory, "credentials.json");
        _protector = dp.CreateProtector("Jellyking.Credentials.v1");
        _creds = Load();
    }

    public event Action? Changed;

    public Task<AuthCredential?> GetCredentialAsync(Guid serviceId, CancellationToken ct = default)
        => Task.FromResult(_creds.TryGetValue(serviceId, out var c) ? c : null);

    public Task<string?> GetSecretAsync(Guid serviceId, CancellationToken ct = default)
        => Task.FromResult(_creds.TryGetValue(serviceId, out var c) ? c.Secret : null);

    public async Task SetCredentialAsync(Guid serviceId, AuthCredential? credential, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (credential is null || credential.IsEmpty)
                _creds.Remove(serviceId);
            else
                _creds[serviceId] = credential;

            await SaveAsync(ct);
        }
        finally { _lock.Release(); }
        Changed?.Invoke();
    }

    public async Task DeleteAsync(Guid serviceId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_creds.Remove(serviceId))
                await SaveAsync(ct);
        }
        finally { _lock.Release(); }
        Changed?.Invoke();
    }

    // ------------------------------------------------------------------ //

    private async Task SaveAsync(CancellationToken ct)
    {
        // Store serviceId -> encrypted JSON blob of the credential record.
        var enc = new Dictionary<string, string>();
        foreach (var kv in _creds)
        {
            var json = JsonSerializer.Serialize(kv.Value, _jsonOptions);
            enc[kv.Key.ToString()] = _protector.Protect(json);
        }

        await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(enc, _jsonOptions), ct);
    }

    private Dictionary<Guid, AuthCredential> Load()
    {
        _lock.Wait();
        try
        {
            if (!File.Exists(_filePath))
                return new Dictionary<Guid, AuthCredential>();

            var json = File.ReadAllText(_filePath);
            var enc = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions)
                      ?? new Dictionary<string, string>();

            var creds = new Dictionary<Guid, AuthCredential>();
            foreach (var kv in enc)
            {
                if (!Guid.TryParse(kv.Key, out var id)) continue;
                try
                {
                    var plain = _protector.Unprotect(kv.Value);
                    var cred = JsonSerializer.Deserialize<AuthCredential>(plain, _jsonOptions);
                    if (cred is not null) creds[id] = cred;
                }
                catch { /* key rotated / corrupted: drop */ }
            }
            return creds;
        }
        catch
        {
            return new Dictionary<Guid, AuthCredential>();
        }
        finally
        {
            _lock.Release();
        }
    }
}
