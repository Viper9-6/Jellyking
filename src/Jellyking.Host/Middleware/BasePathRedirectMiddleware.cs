using Jellyking.Core.Services;

namespace Jellyking.Host.Middleware;

/// <summary>
/// Redirects bare service base paths (e.g. /sonarr) to their trailing-slash
/// form (/sonarr/) so that the YARP route pattern /sonarr/{**catch-all}
/// matches correctly and relative asset URLs in the service UI resolve
/// against the right base. Rebuilds its internal set whenever the service
/// store changes.
/// </summary>
public sealed class BasePathRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceStore _serviceStore;
    private readonly ReaderWriterLockSlim _lock = new();
    private HashSet<string> _basePaths = new(StringComparer.OrdinalIgnoreCase);

    public BasePathRedirectMiddleware(RequestDelegate next, IServiceStore serviceStore)
    {
        _next = next;
        _serviceStore = serviceStore;

        Reload();
        _serviceStore.Changed += OnStoreChanged;
    }

    private void OnStoreChanged()
    {
        Reload();
    }

    private void Reload()
    {
        // Fire-and-forget load from the async store. If it fails, we keep the
        // existing set so the middleware stays safe.
        _ = Task.Run(async () =>
        {
            try
            {
                var services = await _serviceStore.GetAllAsync();
                var paths = new HashSet<string>(
                    services.Where(s => s.Enabled).Select(s => s.BasePath),
                    StringComparer.OrdinalIgnoreCase);

                _lock.EnterWriteLock();
                try
                {
                    _basePaths = paths;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            catch
            {
                // Best-effort: do not crash the request pipeline.
            }
        });
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        HashSet<string> snapshot;
        _lock.EnterReadLock();
        try
        {
            snapshot = _basePaths;
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // If the path exactly matches a service base path (no trailing slash),
        // issue a permanent redirect to the same path with a trailing slash.
        if (snapshot.Contains(path))
        {
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = path + "/";
            return;
        }

        await _next(context);
    }
}
