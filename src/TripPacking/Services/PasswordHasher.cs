using System.Security.Cryptography;
using System.Text;
using TripPacking.Entities;

namespace TripPacking.Services;

public class PasswordHasher : IPasswordHasher
{
    public PasswordHashVersion GetCurrentVersion()
    {
        return PasswordHashVersion.BCrypt;
    }

    public string HashPassword(string password, PasswordHashVersion version = PasswordHashVersion.BCrypt)
    {
        return version switch
        {
            PasswordHashVersion.BCrypt => HashBCrypt(password),
            PasswordHashVersion.Sha256 => HashSha256(password),
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unsupported password hash version")
        };
    }

    public bool VerifyPassword(string password, string hash, PasswordHashVersion version)
    {
        return version switch
        {
            PasswordHashVersion.BCrypt => VerifyBCrypt(password, hash),
            PasswordHashVersion.Sha256 => VerifySha256(password, hash),
            _ => false
        };
    }

    private static string HashBCrypt(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private static bool VerifyBCrypt(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    private static string HashSha256(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifySha256(string password, string hash)
    {
        return HashSha256(password) == hash;
    }
}
