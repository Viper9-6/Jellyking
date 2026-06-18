using System.Security.Cryptography;

namespace Jellyking.Core.Services;

/// <summary>
/// Simple PBKDF2 password hasher. Uses a random 128-bit salt and produces
/// a single string in the format {iterations}.{salt}.{hash}.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int KeyLength  = 256 / 8;

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);
        return $"{Iterations}.{Convert.ToHexString(salt)}.{Convert.ToHexString(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        var salt = Convert.FromHexString(parts[1]);
        var expectedHash = Convert.FromHexString(parts[2]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
