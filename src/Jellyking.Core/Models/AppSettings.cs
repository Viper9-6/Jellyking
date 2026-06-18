namespace Jellyking.Core.Models;

/// <summary>
/// Runtime-editable application settings persisted in data/settings.json.
/// Distinct from <see cref="AppConfig"/> which is bound from appsettings.json
/// at startup and is mostly static.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Dashboard title shown in the header.</summary>
    public string Title { get; set; } = "Jellyking";

    /// <summary>"dark" or "light".</summary>
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// When true, requests originating from localhost (loopback) are
    /// treated as an authenticated admin without requiring a login.
    /// Lets you run Jellyking as a single-user local app.
    /// </summary>
    public bool LocalAccessEnabled { get; set; } = false;
}
