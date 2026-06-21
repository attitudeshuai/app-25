using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPacking.DTOs;
using TripPacking.Services;

namespace TripPacking.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ApiResponse<UserDto>> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.Register(dto);
        return ApiResponse<UserDto>.Success(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ApiResponse<AuthResultDto>> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.Login(dto);
        return ApiResponse<AuthResultDto>.Success(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ApiResponse<UserDto>> GetMe()
    {
        var userId = GetCurrentUserId();
        var result = await _authService.GetCurrentUser(userId);
        return ApiResponse<UserDto>.Success(result);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ApiResponse<UserDto>> UpdateMe([FromBody] UpdateUserDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _authService.UpdateCurrentUser(userId, dto);
        return ApiResponse<UserDto>.Success(result);
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var id) ? id : 0;
    }
}
