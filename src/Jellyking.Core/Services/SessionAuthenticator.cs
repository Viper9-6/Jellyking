using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using Jellyking.Core.Models;
using Microsoft.Extensions.Logging;

namespace Jellyking.Core.Services;

/// <summary>
/// Session-based auto-login. Currently supports qBittorrent: performs a
/// POST to /api/v2/auth/login, parses the SID cookie from the response, and
/// caches it for a short TTL. Injects <c>Cookie: SID=...</c> on proxied
/// requests. Designed to be extended with other session-login schemes.
/// </summary>
public sealed class SessionAuthenticator : ISessionAuthenticator
{
    private readonly ICredentialStore _credentialStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SessionAuthenticator> _logger;

    private readonly ConcurrentDictionary<Guid, CachedSession> _cache = new();
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(20);

    public SessionAuthenticator(
        ICredentialStore credentialStore,
        IHttpClientFactory httpClientFactory,
        ILogger<SessionAuthenticator> logger)
    {
        _credentialStore = credentialStore;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // If a credential changes, drop the cached session so the next request re-logs-in.
        _credentialStore.Changed += () => InvalidateAll();
    }

    public async Task<SessionAuthResult?> GetAuthHeadersAsync(ServiceStatus service, CancellationToken ct = default)
    {
        if (!string.Equals(service.AuthType, "qbittorrent", StringComparison.OrdinalIgnoreCase))
            return null;

        if (_cache.TryGetValue(service.ServiceId, out var cached) && cached.IsValid)
            return new SessionAuthResult(cached.Headers);

        var cred = await _credentialStore.GetCredentialAsync(service.ServiceId, ct);
        if (cred is null || string.IsNullOrWhiteSpace(cred.Username) || string.IsNullOrWhiteSpace(cred.Password))
            return null;

        var sid = await LoginQbittorrentAsync(service, cred, ct);
        if (sid is null)
        {
            _logger.LogWarning("qBittorrent login failed for {Name} ({Host}:{Port}); proxying without session.",
                service.Name, service.Host, service.Port);
            return null;
        }

        var headers = new List<KeyValuePair<string, string>>
        {
            new("Cookie", $"SID={sid}")
        };

        _cache[service.ServiceId] = new CachedSession(headers, DateTimeOffset.UtcNow.Add(SessionTtl));
        return new SessionAuthResult(headers);
    }

    public void Invalidate(Guid serviceId) => _cache.TryRemove(serviceId, out _);

    private void InvalidateAll() => _cache.Clear();

    private async Task<string?> LoginQbittorrentAsync(ServiceStatus svc, AuthCredential cred, CancellationToken ct)
    {
        // qBittorrent requires a Referer/Origin header on the login POST (CSRF
        // protection). Its login endpoint lives under the configured base URL.
        var baseOnUpstream = string.IsNullOrEmpty(svc.BasePath) ? "" : svc.BasePath.TrimEnd('/');
        var loginUrl = $"http://{svc.Host}:{svc.Port}{baseOnUpstream}/api/v2/auth/login";

        try
        {
            var client = _httpClientFactory.CreateClient("session");
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = cred.Username!,
                ["password"] = cred.Password!
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, loginUrl) { Content = content };
            req.Headers.Referrer = new Uri($"http://{svc.Host}:{svc.Port}{baseOnUpstream}/");
            req.Headers.Add("Origin", $"http://{svc.Host}:{svc.Port}");

            using var response = await client.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("qBittorrent login {Url} returned {Status}", loginUrl, (int)response.StatusCode);
                return null;
            }

            // The SID cookie comes back in Set-Cookie. Parse "SID=<value>".
            if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
                return null;

            foreach (var sc in cookies)
            {
                var sid = ParseCookieValue(sc, "SID");
                if (!string.IsNullOrEmpty(sid)) return sid;
            }
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "qBittorrent login request to {Url} failed", loginUrl);
            return null;
        }
    }

    private static string? ParseCookieValue(string setCookie, string name)
    {
        // setCookie looks like: SID=abc123; HttpOnly; Path=/...
        var parts = setCookie.Split(';');
        if (parts.Length == 0) return null;
        var first = parts[0].Trim();
        var eq = first.IndexOf('=');
        if (eq < 0) return null;
        var key = first[..eq].Trim();
        if (!string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) return null;
        return first[(eq + 1)..].Trim();
    }

    private sealed record CachedSession(IReadOnlyList<KeyValuePair<string, string>> Headers, DateTimeOffset Expiry)
    {
        public bool IsValid => DateTimeOffset.UtcNow < Expiry;
    }
}
