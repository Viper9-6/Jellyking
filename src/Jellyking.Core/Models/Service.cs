namespace Jellyking.Core.Models;

/// <summary>
/// A service configured by the admin in Jellyking. Stored in SQLite and
/// used to build YARP routes and the dashboard UI.
/// </summary>
public sealed class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Internal key used in routes and API responses, e.g. "sonarr".</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Human-readable display name shown in the dashboard.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Target host name or IP address.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Target port.</summary>
    public int Port { get; set; }

    /// <summary>
    /// The URL subpath Jellyking serves this service on. Also the BaseUrl
    /// the service itself must be configured with. e.g. "/sonarr"
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// Relative path used for HTTP health probing. A 2xx response means
    /// the service is ready. e.g. "/sonarr/api/v3/system/status"
    /// </summary>
    public string HealthPath { get; set; } = string.Empty;

    /// <summary>Icon filename inside wwwroot/icons/. e.g. "sonarr.svg"</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated URL path prefixes where the service uses WebSocket
    /// connections. Any request whose path starts with one of these is
    /// proxied as a WebSocket.
    /// </summary>
    public string WebSocketPaths { get; set; } = string.Empty;

    /// <summary>Lower number = further left in the dashboard.</summary>
    public int Priority { get; set; } = 100;

    /// <summary>Set to false to hide and skip this service.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How Jellyking authenticates against this service on the user's behalf
    /// so the WebUI loads without a manual login. "none" = no injection,
    /// "apikey" = send X-Api-Key header (Sonarr/Radarr/Prowlarr/Lidarr/Readarr/
    /// Bazarr/Jellyseerr/SABnzbd), "jellyfin" = send X-Emby-Token header.
    /// </summary>
    public string AuthType { get; set; } = "none";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
