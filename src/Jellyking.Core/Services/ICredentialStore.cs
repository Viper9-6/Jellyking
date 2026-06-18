using Jellyking.Core.Models;

namespace Jellyking.Core.Services;

/// <summary>
/// Stores per-service authentication material used by the reverse proxy to
/// auto-login proxied WebUIs. Secrets are encrypted at rest. The auth
/// <em>type</em> lives on the <see cref="Service"/> record; this store holds
/// the secret material keyed by service id.
/// </summary>
public interface ICredentialStore
{
    event Action? Changed;

    /// <summary>The full stored credential for a service, or null if none.</summary>
    Task<AuthCredential?> GetCredentialAsync(Guid serviceId, CancellationToken ct = default);

    /// <summary>Convenience: just the secret (API key / token), or null.</summary>
    Task<string?> GetSecretAsync(Guid serviceId, CancellationToken ct = default);

    /// <summary>Stores (and encrypts) a credential. Pass null/an empty record to clear.</summary>
    Task SetCredentialAsync(Guid serviceId, AuthCredential? credential, CancellationToken ct = default);

    /// <summary>Removes the stored credential for a service (e.g. when the service is deleted).</summary>
    Task DeleteAsync(Guid serviceId, CancellationToken ct = default);
}
