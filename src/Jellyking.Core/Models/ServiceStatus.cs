namespace Jellyking.Core.Models;

/// <summary>
/// The current runtime status of a single service, produced by
/// ServiceDetector after each probe cycle.
/// </summary>
public sealed record ServiceStatus
{
    public required string Id { get; init; }

    /// <summary>Persisted Guid of the service (used for admin edits).</summary>
    public Guid ServiceId { get; init; }
    public required string Name { get; init; }
    public required string BasePath { get; init; }
    public required string Icon { get; init; }
    public required int Priority { get; init; }

    /// <summary>Resolved host — either from config override or detection target host.</summary>
    public required string Host { get; init; }

    /// <summary>Resolved port — either from config override or the service default.</summary>
    public required int Port { get; init; }

    /// <summary>True when the last TCP probe + HTTP health check both succeeded.</summary>
    public required bool IsUp { get; init; }

    /// <summary>Auth scheme Jellyking injects for auto-login. "none"|"apikey"|"jellyfin".</summary>
    public string AuthType { get; init; } = "none";

    /// <summary>
    /// Secret (API key / Jellyfin token) injected as a request header by the
    /// proxy. Stays inside the process; never serialized to the public API
    /// (ServiceStatusDto omits it).
    /// </summary>
    public string? AuthSecret { get; init; }

    /// <summary>Machine-readable reason when IsUp is false.</summary>
    public string? DownReason { get; init; }

    /// <summary>UTC timestamp of the last probe attempt.</summary>
    public DateTimeOffset LastChecked { get; init; } = DateTimeOffset.UtcNow;
}
