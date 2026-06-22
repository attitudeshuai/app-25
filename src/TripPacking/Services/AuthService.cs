using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public AuthService(
        IUserRepository userRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<UserDto> Register(RegisterDto dto)
    {
        var existingByEmail = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingByEmail != null)
            throw new InvalidOperationException("Email is already registered");

        var existingByUsername = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existingByUsername != null)
            throw new InvalidOperationException("Username is already taken");

        var currentVersion = _passwordHasher.GetCurrentVersion();
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = _passwordHasher.HashPassword(dto.Password, currentVersion),
            PasswordHashVersion = currentVersion,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<AuthResultDto> Login(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        var isValid = _passwordHasher.VerifyPassword(dto.Password, user.PasswordHash, user.PasswordHashVersion);
        if (!isValid)
            throw new UnauthorizedAccessException("Invalid email or password");

        var needsUpgrade = user.PasswordHashVersion != _passwordHasher.GetCurrentVersion();
        if (needsUpgrade)
        {
            await UpgradePasswordHash(user, dto.Password);
        }

        var token = _jwtService.GenerateToken(user.Id, user.Username, user.Email);
        return new AuthResultDto
        {
            Token = token,
            User = _mapper.Map<UserDto>(user),
            PasswordNeedsReset = needsUpgrade
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

    public async Task ChangePassword(int userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var isValid = _passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash, user.PasswordHashVersion);
        if (!isValid)
            throw new UnauthorizedAccessException("Current password is incorrect");

        var currentVersion = _passwordHasher.GetCurrentVersion();
        user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword, currentVersion);
        user.PasswordHashVersion = currentVersion;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }

    private async Task UpgradePasswordHash(User user, string password)
    {
        var currentVersion = _passwordHasher.GetCurrentVersion();
        user.PasswordHash = _passwordHasher.HashPassword(password, currentVersion);
        user.PasswordHashVersion = currentVersion;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
    }
}
