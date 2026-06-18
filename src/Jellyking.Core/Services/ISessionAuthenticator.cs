using Jellyking.Core.Models;

namespace Jellyking.Core.Services;

/// <summary>
/// A set of HTTP headers to inject into a proxied request so the upstream
/// service treats it as already authenticated (the result of a server-side
/// login for session-based services like qBittorrent).
/// </summary>
public sealed record SessionAuthResult(IReadOnlyList<KeyValuePair<string, string>> Headers);

/// <summary>
/// Performs and caches server-side logins for session-based services so
/// Jellyking can inject the resulting cookie/token into proxied requests.
/// </summary>
public interface ISessionAuthenticator
{
    /// <summary>
    /// Returns headers to set on the proxied request, or null when no session
    /// auth applies (e.g. the service isn't session-based, or login failed).
    /// </summary>
    Task<SessionAuthResult?> GetAuthHeadersAsync(ServiceStatus service, CancellationToken ct = default);

    /// <summary>Drop any cached session for a service (e.g. after a credential change).</summary>
    void Invalidate(Guid serviceId);
}
