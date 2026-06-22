using TripPacking.Entities;

namespace TripPacking.Services;

public interface IPasswordHasher
{
    string HashPassword(string password, PasswordHashVersion version = PasswordHashVersion.BCrypt);
    bool VerifyPassword(string password, string hash, PasswordHashVersion version);
    PasswordHashVersion GetCurrentVersion();
}
