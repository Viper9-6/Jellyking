using Jellyking.Core.Models;

namespace Jellyking.Core.Services;

/// <summary>
/// Pre-filled templates for well-known media-stack services shown in the
/// "Add service" modal. Admins can still override every field.
/// </summary>
public static class ServiceTemplates
{
    public static IReadOnlyList<Service> All { get; } = new List<Service>
    {
        new()
        {
            Slug = "jellyfin", Name = "Jellyfin", Host = "localhost", Port = 8096,
            BasePath = "/jellyfin", HealthPath = "/jellyfin/health", Icon = "jellyfin.svg",
            WebSocketPaths = "/jellyfin/socket", Priority = 10
        },
        new()
        {
            Slug = "sonarr", Name = "Sonarr", Host = "localhost", Port = 8989,
            BasePath = "/sonarr", HealthPath = "/sonarr/api/v3/system/status", Icon = "sonarr.svg",
            WebSocketPaths = "/sonarr/signalr", Priority = 20
        },
        new()
        {
            Slug = "radarr", Name = "Radarr", Host = "localhost", Port = 7878,
            BasePath = "/radarr", HealthPath = "/radarr/api/v3/system/status", Icon = "radarr.svg",
            WebSocketPaths = "/radarr/signalr", Priority = 30
        },
        new()
        {
            Slug = "prowlarr", Name = "Prowlarr", Host = "localhost", Port = 9696,
            BasePath = "/prowlarr", HealthPath = "/prowlarr/api/v1/health", Icon = "prowlarr.svg",
            WebSocketPaths = "", Priority = 40
        },
        new()
        {
            Slug = "lidarr", Name = "Lidarr", Host = "localhost", Port = 8686,
            BasePath = "/lidarr", HealthPath = "/lidarr/api/v1/health", Icon = "lidarr.svg",
            WebSocketPaths = "/lidarr/signalr", Priority = 50
        },
        new()
        {
            Slug = "readarr", Name = "Readarr", Host = "localhost", Port = 8787,
            BasePath = "/readarr", HealthPath = "/readarr/api/v1/health", Icon = "readarr.svg",
            WebSocketPaths = "/readarr/signalr", Priority = 60
        },
        new()
        {
            Slug = "jellyseerr", Name = "Jellyseerr", Host = "localhost", Port = 5055,
            BasePath = "/jellyseerr", HealthPath = "/jellyseerr/api/v1/status", Icon = "jellyseerr.svg",
            WebSocketPaths = "/jellyseerr/socket.io", Priority = 70
        },
        new()
        {
            Slug = "bazarr", Name = "Bazarr", Host = "localhost", Port = 6767,
            BasePath = "/bazarr", HealthPath = "/bazarr/api/system/status", Icon = "bazarr.svg",
            WebSocketPaths = "", Priority = 80
        },
        new()
        {
            Slug = "qbittorrent", Name = "qBittorrent", Host = "localhost", Port = 8080,
            BasePath = "/qbit", HealthPath = "/qbit/api/v2/app/version", Icon = "qbittorrent.svg",
            WebSocketPaths = "", Priority = 90
        },
        new()
        {
            Slug = "sabnzbd", Name = "SABnzbd", Host = "localhost", Port = 8080,
            BasePath = "/sabnzbd", HealthPath = "/sabnzbd/api", Icon = "sabnzbd.svg",
            WebSocketPaths = "", Priority = 100
        },
    };

    public static Service? Find(string slug)
    {
        return All.FirstOrDefault(t =>
            string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }
}
