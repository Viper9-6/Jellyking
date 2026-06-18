using System.Text.Json;

namespace Jellyking.Core.Services;

/// <summary>
/// Helpers for JSON-backed file stores. All IO is async and serialized
/// through a SemaphoreSlim to keep the file consistent.
/// </summary>
public abstract class JsonStoreBase<T>
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    protected abstract string FilePath { get; }

    protected async Task<IReadOnlyList<T>> LoadAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(FilePath))
                return new List<T>();

            await using var stream = File.OpenRead(FilePath);
            var data = await JsonSerializer.DeserializeAsync<List<T>>(stream, _jsonOptions, ct);
            return data ?? new List<T>();
        }
        finally
        {
            _lock.Release();
        }
    }

    protected async Task SaveAsync(IEnumerable<T> items, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            await using var stream = File.Create(FilePath);
            await JsonSerializer.SerializeAsync(stream, items, _jsonOptions, ct);
        }
        finally
        {
            _lock.Release();
        }
    }
}
