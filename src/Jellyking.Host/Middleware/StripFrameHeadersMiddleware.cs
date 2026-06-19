using Jellyking.Core.Services;

namespace Jellyking.Host.Middleware;

/// <summary>
/// Removes the <c>X-Frame-Options</c> and <c>Content-Security-Policy</c>
/// headers from responses served under a configured service's base path, so
/// the proxied WebUI can be embedded in Jellyking's in-app iframe view.
/// Without this, apps like Sonarr/Jellyfin/qBittorrent send
/// X-Frame-Options: SAMEORIGIN/DENY or a CSP frame-ancestors directive that
/// blocks same-origin embedding. Jellyking's own pages don't set these
/// headers, so this only affects proxied service responses.
/// </summary>
public sealed class StripFrameHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ServiceDetector _detector;

    public StripFrameHeadersMiddleware(RequestDelegate next, ServiceDetector detector)
    {
        _next = next;
        _detector = detector;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Only strip for paths under a configured service base path.
        var isServicePath = _detector.GetStatuses().Any(s =>
            path.Equals(s.BasePath, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(s.BasePath + "/", StringComparison.OrdinalIgnoreCase));

        if (isServicePath)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Remove("X-Frame-Options");
                context.Response.Headers.Remove("Content-Security-Policy");
                context.Response.Headers.Remove("Content-Security-Policy-Report-Only");
                // Allow the embedded app to be framed by Jellyking (same origin).
                context.Response.Headers.ContentSecurityPolicy = "frame-ancestors 'self'";
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}
