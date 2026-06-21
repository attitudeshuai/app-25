namespace TripPacking.Services;

public interface IJwtService
{
    string GenerateToken(int userId, string username, string email);
}
