using System.Text.Json;
using Jellyking.Core.Models;

namespace Jellyking.Core.Services;

/// <summary>
/// Persists <see cref="AppSettings"/> as a single JSON object in
/// data/settings.json. Thread-safe via a semaphore; broadcasts a Changed
/// event whenever settings are updated.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private AppSettings _current = new();

    public JsonSettingsStore(string dataDirectory)
    {
        _filePath = Path.Combine(dataDirectory, "settings.json");
        // Load synchronously on startup so the first request sees persisted values.
        _current = Load();
    }

    public event Action? Changed;

    public Task<AppSettings> GetAsync(CancellationToken ct = default) => Task.FromResult(_current);

    public async Task<AppSettings> UpdateAsync(AppSettings settings, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            settings.Title = string.IsNullOrWhiteSpace(settings.Title) ? "Jellyking" : settings.Title.Trim();
            settings.Theme = string.IsNullOrWhiteSpace(settings.Theme) ? "dark" : settings.Theme.Trim();

            _current = settings;
            await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(_current, _jsonOptions), ct);
        }
        finally
        {
            _lock.Release();
        }

        Changed?.Invoke();
        return _current;
    }

    private AppSettings Load()
    {
        _lock.Wait();
        try
        {
            if (!File.Exists(_filePath))
                return new AppSettings();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
        finally
        {
            _lock.Release();
        }
    }
}
