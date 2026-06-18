using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Serilog;

namespace Jellyking.Host;

/// <summary>
/// Loads or generates the TLS certificate for HTTPS. When a <c>.pfx</c> path
/// is configured it is loaded directly; otherwise a self-signed certificate
/// for localhost / 127.0.0.1 is generated once into the data directory and
/// reused on subsequent starts.
/// </summary>
public static class TlsCertLoader
{
    public static X509Certificate2 Load(string dataDirectory, string? certPath, string? certPassword)
    {
        if (!string.IsNullOrWhiteSpace(certPath))
        {
            Log.Information("Loading TLS certificate from {Path}", certPath);
            return new X509Certificate2(
                Path.GetFullPath(certPath),
                certPassword ?? string.Empty,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
        }

        var pfxPath = Path.Combine(dataDirectory, "jellyking.pfx");
        var pwdPath = Path.Combine(dataDirectory, "jellyking.pfx.pwd");

        if (File.Exists(pfxPath) && File.Exists(pwdPath))
        {
            var pwd = File.ReadAllText(pwdPath).Trim();
            Log.Information("Reusing existing self-signed TLS certificate at {Path}", pfxPath);
            return new X509Certificate2(pfxPath, pwd, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
        }

        Log.Information("Generating a new self-signed TLS certificate at {Path}", pfxPath);
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=Jellyking", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: false));
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, critical: true));

        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddIpAddress(IPAddress.Loopback);
        san.AddIpAddress(IPAddress.IPv6Loopback);
        req.CertificateExtensions.Add(san.Build());

        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        File.WriteAllBytes(pfxPath, cert.Export(X509ContentType.Pfx, password));
        File.WriteAllText(pwdPath, password);
        // Restrict the password file perms best-effort.
        try { if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) File.SetUnixFileMode(pwdPath, UnixFileMode.UserRead | UnixFileMode.UserWrite); } catch { }

        return new X509Certificate2(pfxPath, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
    }
}
