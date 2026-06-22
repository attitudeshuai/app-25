using TripPacking.DTOs;

namespace TripPacking.Services;

public interface IAuthService
{
    Task<UserDto> Register(RegisterDto dto);
    Task<AuthResultDto> Login(LoginDto dto);
    Task<UserDto> GetCurrentUser(int userId);
    Task<UserDto> UpdateCurrentUser(int userId, UpdateUserDto dto);
    Task ChangePassword(int userId, ChangePasswordDto dto);
}
