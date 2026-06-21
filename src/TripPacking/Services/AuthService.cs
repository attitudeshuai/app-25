using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;

    public AuthService(IUserRepository userRepository, IJwtService jwtService, IMapper mapper)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _mapper = mapper;
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    public async Task<UserDto> Register(RegisterDto dto)
    {
        var existingByEmail = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingByEmail != null)
            throw new InvalidOperationException("Email is already registered");

        var existingByUsername = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existingByUsername != null)
            throw new InvalidOperationException("Username is already taken");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<AuthResultDto> Login(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        var token = _jwtService.GenerateToken(user.Id, user.Username, user.Email);
        return new AuthResultDto
        {
            Token = token,
            User = _mapper.Map<UserDto>(user)
        };
    }

    public async Task<UserDto> GetCurrentUser(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateCurrentUser(int userId, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        if (!string.IsNullOrWhiteSpace(dto.Username))
        {
            var existing = await _userRepository.GetByUsernameAsync(dto.Username);
            if (existing != null && existing.Id != userId)
                throw new InvalidOperationException("Username is already taken");
            user.Username = dto.Username;
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var existing = await _userRepository.GetByEmailAsync(dto.Email);
            if (existing != null && existing.Id != userId)
                throw new InvalidOperationException("Email is already registered");
            user.Email = dto.Email;
        }

        if (dto.Avatar != null)
            user.Avatar = dto.Avatar;

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserDto>(user);
    }
}
