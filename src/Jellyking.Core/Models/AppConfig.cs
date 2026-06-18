namespace Jellyking.Core.Models;

/// <summary>
/// Root configuration object. Bound from the "Jellyking" section of
/// appsettings.json (or environment variables with JELLYKING__ prefix).
/// </summary>
public sealed class AppConfig
{
    public const string SectionName = "Jellyking";

    public ServerConfig Server { get; set; } = new();
    public DetectionConfig Detection { get; set; } = new();
    public UiConfig Ui { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();

    /// <summary>
    /// Per-service overrides keyed by service ID (e.g. "sonarr").
    /// Services not listed here use their ServiceDefinition defaults.
    /// </summary>
    public Dictionary<string, ServiceOverrideConfig> Services { get; set; } = new();
}

public sealed class ServerConfig
{
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 5656;
}

public sealed class DetectionConfig
{
    /// <summary>
    /// The hostname where Jellyking looks for services. Use "localhost"
    /// for host-network or bare-metal. In Docker bridge mode, point this
    /// at your Docker gateway or use per-service host overrides.
    /// </summary>
    public string TargetHost { get; set; } = "localhost";

    /// <summary>Seconds between probe cycles.</summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>Per-service connection timeout in milliseconds.</summary>
    public int TimeoutMs { get; set; } = 2000;
}

public sealed class UiConfig
{
    public string Theme { get; set; } = "dark";
    public string Title { get; set; } = "Jellyking";
}

/// <summary>
/// Optional per-service config block. Every field is nullable so the
/// detector knows whether the user explicitly set it vs. using defaults.
/// </summary>
public sealed class ServiceOverrideConfig
{
    /// <summary>Set to false to completely hide and skip this service.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Override the host for this specific service only.</summary>
    public string? Host { get; set; }

    /// <summary>Override the port if the service runs on a non-default port.</summary>
    public int? Port { get; set; }

    /// <summary>Override the display name shown in the tab bar.</summary>
    public string? Label { get; set; }
}

/// <summary>
/// TLS / HTTPS hardening. Off by default so local single-user setups keep
/// working over plain HTTP; enable for any non-localhost exposure.
/// </summary>
public sealed class SecurityConfig
{
    /// <summary>When true, serve HTTPS (with HTTP->HTTPS redirect, HSTS, secure cookie).
    /// A self-signed cert is auto-generated in data/ when no CertPath is set.</summary>
    public bool UseHttps { get; set; } = false;

    /// <summary>HTTP listener used for redirect-to-HTTPS when UseHttps is true.</summary>
    public int HttpPort { get; set; } = 5656;

    /// <summary>HTTPS listener port when UseHttps is true.</summary>
    public int HttpsPort { get; set; } = 5657;

    /// <summary>Optional path to a .pfx certificate. Null = auto-generate self-signed.</summary>
    public string? CertPath { get; set; }

    /// <summary>Password for the .pfx at CertPath (ignored for auto-generated cert).</summary>
    public string? CertPassword { get; set; }

    /// <summary>Emit the Strict-Transport-Security header over HTTPS.</summary>
    public bool Hsts { get; set; } = true;
}
