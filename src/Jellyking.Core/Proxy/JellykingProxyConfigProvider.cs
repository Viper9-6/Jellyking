using Jellyking.Core.Models;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Jellyking.Core.Proxy;

/// <summary>
/// Implements YARP's <see cref="IProxyConfigProvider"/> so that Jellyking
/// can hot-reload proxy routes whenever the ServiceDetector discovers that
/// a service has come up or gone down — no application restart needed.
///
/// How it works:
///   1. YARP calls GetConfig() on startup to get the initial set of routes.
///   2. When ServiceDetector detects a change it calls Update().
///   3. Update() replaces the config object and calls SignalChange() on the
///      old one, which cancels its IChangeToken.
///   4. YARP sees the token cancellation, calls GetConfig() again, and
///      applies the new routing table — all in the background.
/// </summary>
public sealed class JellykingProxyConfigProvider : IProxyConfigProvider
{
    // volatile so reads from any thread always see the latest reference.
    private volatile JellykingProxyConfig _current;

    public JellykingProxyConfigProvider()
    {
        // Start with an empty config; ServiceDetector populates it at startup.
        _current = BuildConfig(Enumerable.Empty<ServiceStatus>());
    }

    /// <inheritdoc />
    public IProxyConfig GetConfig() => _current;

    /// <summary>
    /// Replaces the active config with a new one built from the given
    /// service statuses and signals YARP to reload.
    /// Only services where <see cref="ServiceStatus.IsUp"/> is true get
    /// a proxy route — down services are simply not reachable via Jellyking.
    /// </summary>
    public void Update(IEnumerable<ServiceStatus> allStatuses)
    {
        var oldConfig = _current;
        _current = BuildConfig(allStatuses.Where(s => s.IsUp));
        oldConfig.SignalChange();
    }

    // ------------------------------------------------------------------ //

    private static JellykingProxyConfig BuildConfig(IEnumerable<ServiceStatus> upServices)
    {
        var routes   = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();

        foreach (var svc in upServices)
        {
            // Route: match any request whose path starts with /basepath/
            routes.Add(new RouteConfig
            {
                RouteId   = $"route-{svc.Id}",
                ClusterId = $"cluster-{svc.Id}",
                Match     = new RouteMatch
                {
                    // {**catch-all} matches the empty string too, so /sonarr/
                    // and /sonarr/api/... both match.
                    Path = $"{svc.BasePath}/{{**catch-all}}"
                },
                // No path transforms — we preserve the full path so the
                // backend service (configured with a matching BaseUrl) can
                // handle it correctly.
                Transforms = BuildAuthTransforms(svc),
            });

            // Cluster: single destination at the service's local address.
            clusters.Add(new ClusterConfig
            {
                ClusterId    = $"cluster-{svc.Id}",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    [$"dest-{svc.Id}"] = new DestinationConfig
                    {
                        Address = $"http://{svc.Host}:{svc.Port}"
                    }
                },
                // Let YARP handle WebSocket upgrade — it does this automatically
                // when the Upgrade: websocket header is present.
                HttpRequest  = new ForwarderRequestConfig
                {
                    ActivityTimeout = TimeSpan.FromMinutes(5)
                }
            });
        }

        return new JellykingProxyConfig(routes, clusters);
    }


    /// <summary>
    /// Builds YARP request transforms that inject stored credentials so the
    /// proxied WebUI loads already authenticated. Returns an empty list when
    /// the service has no auth configured.
    /// </summary>
    private static List<Dictionary<string, string>> BuildAuthTransforms(ServiceStatus svc)
    {
        var transforms = new List<Dictionary<string, string>>();

        if (string.IsNullOrWhiteSpace(svc.AuthSecret) ||
            string.Equals(svc.AuthType, "none", StringComparison.OrdinalIgnoreCase))
        {
            return transforms;
        }

        // Header-based auth (static secrets). Session-based types like
        // "qbittorrent" are handled by SessionAuthMiddleware, not here.
        var header = svc.AuthType.ToLowerInvariant() switch
        {
            "apikey"   => "X-Api-Key",    // Sonarr/Radarr/Prowlarr/Lidarr/Readarr/Bazarr/Jellyseerr/SABnzbd
            "jellyfin" => "X-Emby-Token",
            _          => null
        };

        if (header is not null)
        {
            transforms.Add(new Dictionary<string, string>
            {
                ["RequestHeader"] = header,
                ["Set"]           = svc.AuthSecret!,
            });
        }

        return transforms;
    }
}

// -------------------------------------------------------------------------- //

/// <summary>
/// Immutable snapshot of YARP route + cluster configuration, with a
/// CancellationToken-backed IChangeToken so YARP knows when to reload.
/// </summary>
internal sealed class JellykingProxyConfig : IProxyConfig
{
    private readonly CancellationTokenSource _cts = new();

    public JellykingProxyConfig(
        IReadOnlyList<RouteConfig>   routes,
        IReadOnlyList<ClusterConfig> clusters)
    {
        Routes      = routes;
        Clusters    = clusters;
        ChangeToken = new CancellationChangeToken(_cts.Token);
    }

    public IReadOnlyList<RouteConfig>   Routes      { get; }
    public IReadOnlyList<ClusterConfig> Clusters    { get; }
    public IChangeToken                 ChangeToken { get; }

    /// <summary>
    /// Cancels this config's token, which causes YARP to call
    /// <see cref="IProxyConfigProvider.GetConfig"/> on the provider and
    /// pick up the replacement config.
    /// </summary>
    public void SignalChange() => _cts.Cancel();
}
