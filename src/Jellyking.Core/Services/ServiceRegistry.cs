using Jellyking.Core.Models;

namespace Jellyking.Core.Services;

/// <summary>
/// The static catalogue of every media stack service Jellyking knows about.
/// Add new services here — no other code changes are required.
/// </summary>
public static class ServiceRegistry
{
    public static readonly IReadOnlyList<ServiceDefinition> All = new List<ServiceDefinition>
    {
        new()
        {
            Id           = "jellyfin",
            Name         = "Jellyfin",
            DefaultPort  = 8096,
            BasePath     = "/jellyfin",
            HealthPath   = "/jellyfin/health",
            Icon         = "jellyfin.svg",
            WebSocketPaths = ["/jellyfin/socket"],
            Priority     = 1,
        },
        new()
        {
            Id           = "sonarr",
            Name         = "Sonarr",
            DefaultPort  = 8989,
            BasePath     = "/sonarr",
            HealthPath   = "/sonarr/api/v3/system/status",
            Icon         = "sonarr.svg",
            WebSocketPaths = ["/sonarr/signalr"],
            Priority     = 2,
        },
        new()
        {
            Id           = "radarr",
            Name         = "Radarr",
            DefaultPort  = 7878,
            BasePath     = "/radarr",
            HealthPath   = "/radarr/api/v3/system/status",
            Icon         = "radarr.svg",
            WebSocketPaths = ["/radarr/signalr"],
            Priority     = 3,
        },
        new()
        {
            Id           = "prowlarr",
            Name         = "Prowlarr",
            DefaultPort  = 9696,
            BasePath     = "/prowlarr",
            HealthPath   = "/prowlarr/api/v1/system/status",
            Icon         = "prowlarr.svg",
            WebSocketPaths = ["/prowlarr/signalr"],
            Priority     = 4,
        },
        new()
        {
            Id           = "lidarr",
            Name         = "Lidarr",
            DefaultPort  = 8686,
            BasePath     = "/lidarr",
            HealthPath   = "/lidarr/api/v1/system/status",
            Icon         = "lidarr.svg",
            WebSocketPaths = ["/lidarr/signalr"],
            Priority     = 5,
        },
        new()
        {
            Id           = "readarr",
            Name         = "Readarr",
            DefaultPort  = 8787,
            BasePath     = "/readarr",
            HealthPath   = "/readarr/api/v1/system/status",
            Icon         = "readarr.svg",
            WebSocketPaths = ["/readarr/signalr"],
            Priority     = 6,
        },
        new()
        {
            Id           = "jellyseerr",
            Name         = "Jellyseerr",
            DefaultPort  = 5055,
            BasePath     = "/jellyseerr",
            HealthPath   = "/jellyseerr/api/v1/status",
            Icon         = "jellyseerr.svg",
            WebSocketPaths = [],
            Priority     = 7,
        },
        new()
        {
            Id           = "bazarr",
            Name         = "Bazarr",
            DefaultPort  = 6767,
            BasePath     = "/bazarr",
            HealthPath   = "/bazarr/api/system/status",
            Icon         = "bazarr.svg",
            WebSocketPaths = ["/bazarr/socket.io"],
            Priority     = 8,
        },
        new()
        {
            Id           = "qbittorrent",
            Name         = "qBittorrent",
            DefaultPort  = 8080,
            BasePath     = "/qbit",
            HealthPath   = "/qbit/api/v2/app/version",
            Icon         = "qbittorrent.svg",
            WebSocketPaths = [],
            Priority     = 9,
        },
        new()
        {
            Id           = "sabnzbd",
            Name         = "SABnzbd",
            DefaultPort  = 8085,
            BasePath     = "/sabnzbd",
            HealthPath   = "/sabnzbd/api?mode=version&output=json",
            Icon         = "sabnzbd.svg",
            WebSocketPaths = [],
            Priority     = 10,
        },
    };

    private static readonly Dictionary<string, ServiceDefinition> _byId =
        All.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);

    public static ServiceDefinition? GetById(string id) =>
        _byId.TryGetValue(id, out var def) ? def : null;
}
