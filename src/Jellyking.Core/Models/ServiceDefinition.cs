namespace Jellyking.Core.Models;

/// <summary>
/// Describes a known media stack service — its defaults, health check
/// path, and which URL paths use WebSockets. Instances are immutable
/// and defined in ServiceRegistry.
/// </summary>
public sealed record ServiceDefinition
{
    /// <summary>Internal key used in config, routes, and API responses. e.g. "sonarr"</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name shown in the tab bar.</summary>
    public required string Name { get; init; }

    /// <summary>The port this service runs on out of the box.</summary>
    public required int DefaultPort { get; init; }

    /// <summary>
    /// The URL subpath Jellyking serves this service on. Also the BaseUrl
    /// the service itself must be configured with. e.g. "/sonarr"
    /// </summary>
    public required string BasePath { get; init; }

    /// <summary>
    /// Relative path used for HTTP health probing. A 2xx response means
    /// the service is ready. e.g. "/sonarr/api/v3/system/status"
    /// </summary>
    public required string HealthPath { get; init; }

    /// <summary>Icon filename inside wwwroot/icons/. e.g. "sonarr.svg"</summary>
    public required string Icon { get; init; }

    /// <summary>
    /// URL path prefixes where the service uses WebSocket connections.
    /// Any request whose path starts with one of these is proxied as WS.
    /// </summary>
    public required IReadOnlyList<string> WebSocketPaths { get; init; }

    /// <summary>Lower number = further left in the tab bar.</summary>
    public required int Priority { get; init; }
}
