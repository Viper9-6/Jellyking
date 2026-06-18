using System.Text.Json.Serialization;

namespace Jellyking.Core.Models;

/// <summary>
/// Stored authentication material for a single service, encrypted at rest.
/// Only the fields relevant to the service's <see cref="Service.AuthType"/>
/// are populated; the rest are null.
/// </summary>
public sealed class AuthCredential
{
    /// <summary>API key / token for "apikey" and "jellyfin" auth types.</summary>
    public string? Secret { get; set; }

    /// <summary>Username for session-login auth types (e.g. "qbittorrent").</summary>
    public string? Username { get; set; }

    /// <summary>Password for session-login auth types.</summary>
    public string? Password { get; set; }

    [JsonIgnore]
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Secret) &&
        string.IsNullOrWhiteSpace(Username) &&
        string.IsNullOrWhiteSpace(Password);
}
