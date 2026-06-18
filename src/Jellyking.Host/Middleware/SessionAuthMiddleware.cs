using Jellyking.Core.Models;
using Jellyking.Core.Services;

namespace Jellyking.Host.Middleware;

/// <summary>
/// For session-based services (currently qBittorrent), injects the cookie
/// obtained from a server-side login into the proxied request so the
/// upstream WebUI loads already authenticated. Runs before YARP forwards
/// the request. Header-based auth (API key / Jellyfin token) is handled by
/// YARP transforms instead.
/// </summary>
public sealed class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ServiceDetector _detector;
    private readonly ISessionAuthenticator _authenticator;

    public SessionAuthMiddleware(RequestDelegate next, ServiceDetector detector, ISessionAuthenticator authenticator)
    {
        _next = next;
        _detector = detector;
        _authenticator = authenticator;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Only session-based services need this; find a matching up service.
        var svc = _detector.GetStatuses().FirstOrDefault(s =>
            string.Equals(s.AuthType, "qbittorrent", StringComparison.OrdinalIgnoreCase) &&
            (path.Equals(s.BasePath, StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith(s.BasePath + "/", StringComparison.OrdinalIgnoreCase)));

        if (svc is not null)
        {
            var auth = await _authenticator.GetAuthHeadersAsync(svc, context.RequestAborted);
            if (auth is not null)
            {
                foreach (var (name, value) in auth.Headers)
                    context.Request.Headers[name] = value;

                // qBittorrent validates Referer/Origin for CSRF on POSTs; present
                // the request as same-origin to the upstream so writes aren't
                // rejected. (The browser's own Origin points at Jellyking.)
                var origin = $"http://{svc.Host}:{svc.Port}{svc.BasePath.TrimEnd('/')}";
                context.Request.Headers["Referer"] = origin + "/";
                context.Request.Headers["Origin"] = origin;
            }
        }

        await _next(context);
    }
}
