using System.Security.Cryptography;
using CompanyApi.Application.Services;

namespace CompanyApi.Infrastructure.Security;

/// <summary>PBKDF2-SHA256 with a random salt. Stored as "iterations.salt.hash" (base64 parts).</summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 600_000; // OWASP guidance for PBKDF2-HMAC-SHA256; stored in the hash, so it can be raised later
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string? stored)
    {
        if (stored is null)
        {
            Rfc2898DeriveBytes.Pbkdf2(password, new byte[SaltSize], Iterations, HashAlgorithmName.SHA256, HashSize);
            return false;
        }

        var parts = stored.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;

        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(parts[1]), iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
