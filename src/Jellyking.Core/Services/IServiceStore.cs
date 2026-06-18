using Jellyking.Core.Models;

namespace Jellyking.Core.Services;

public interface IServiceStore
{
    event Action? Changed;

    Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken ct = default);
    Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Service?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Service> AddAsync(Service service, CancellationToken ct = default);
    Task<Service> UpdateAsync(Service service, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
}
