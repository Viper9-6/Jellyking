using Jellyking.Core.Models;

namespace Jellyking.Core.Services;

public interface ISettingsStore
{
    event Action? Changed;

    Task<AppSettings> GetAsync(CancellationToken ct = default);
    Task<AppSettings> UpdateAsync(AppSettings settings, CancellationToken ct = default);
}
