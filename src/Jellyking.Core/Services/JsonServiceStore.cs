using Jellyking.Core.Models;

namespace Jellyking.Core.Services;

public sealed class JsonServiceStore : JsonStoreBase<Service>, IServiceStore
{
    private readonly string _filePath;

    public JsonServiceStore(string dataDirectory)
    {
        _filePath = Path.Combine(dataDirectory, "services.json");
    }

    protected override string FilePath => _filePath;

    public event Action? Changed;

    public async Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken ct = default)
    {
        return await LoadAsync(ct);
    }

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        return all.FirstOrDefault(s => s.Id == id);
    }

    public async Task<Service?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        return all.FirstOrDefault(s =>
            string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Service> AddAsync(Service service, CancellationToken ct = default)
    {
        var all = (await LoadAsync(ct)).ToList();
        service.Id = Guid.NewGuid();
        service.CreatedAt = DateTimeOffset.UtcNow;
        service.UpdatedAt = service.CreatedAt;
        all.Add(service);
        await SaveAsync(all, ct);
        RaiseChanged();
        return service;
    }

    public async Task<Service> UpdateAsync(Service service, CancellationToken ct = default)
    {
        var all = (await LoadAsync(ct)).ToList();
        var idx = all.FindIndex(s => s.Id == service.Id);
        if (idx < 0)
            throw new InvalidOperationException($"Service {service.Id} not found.");

        service.UpdatedAt = DateTimeOffset.UtcNow;
        all[idx] = service;
        await SaveAsync(all, ct);
        RaiseChanged();
        return service;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var all = (await LoadAsync(ct)).ToList();
        var removed = all.RemoveAll(s => s.Id == id);
        if (removed > 0)
        {
            await SaveAsync(all, ct);
            RaiseChanged();
        }
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        return all.Any(s =>
            string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    private void RaiseChanged()
    {
        Changed?.Invoke();
    }
}
