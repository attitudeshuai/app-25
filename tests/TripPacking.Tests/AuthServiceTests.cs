using AutoMapper;
using FluentAssertions;
using Moq;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;
using TripPacking.Services;
using Xunit;

namespace TripPacking.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _mockMapper = new Mock<IMapper>();
        _authService = new AuthService(_mockUserRepository.Object, _mockJwtService.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Test_Register_WithValidData_ReturnsUserDto()
    {
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123"
        };
        var user = new User
        {
            Id = 1,
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = AuthService.HashPassword(registerDto.Password)
        };
        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(registerDto.Email)).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.GetByUsernameAsync(registerDto.Username)).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(userDto);

        var result = await _authService.Register(registerDto);

        result.Should().NotBeNull();
        result.Username.Should().Be(registerDto.Username);
        result.Email.Should().Be(registerDto.Email);
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Test_Register_WithDuplicateEmail_ThrowsException()
    {
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "duplicate@example.com",
            Password = "password123"
        };
        var existingUser = new User
        {
            Id = 1,
            Username = "existing",
            Email = registerDto.Email
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(registerDto.Email)).ReturnsAsync(existingUser);

        var act = async () => await _authService.Register(registerDto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Test_Login_WithValidCredentials_ReturnsAuthResult()
    {
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "password123"
        };
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = loginDto.Email,
            PasswordHash = AuthService.HashPassword(loginDto.Password)
        };
        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email
        };
        var token = "test-jwt-token";

        _mockUserRepository.Setup(r => r.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);
        _mockJwtService.Setup(j => j.GenerateToken(user.Id, user.Username, user.Email)).Returns(token);
        _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

        var result = await _authService.Login(loginDto);

        result.Should().NotBeNull();
        result.Token.Should().Be(token);
        result.User.Should().BeEquivalentTo(userDto);
        _mockJwtService.Verify(j => j.GenerateToken(user.Id, user.Username, user.Email), Times.Once);
    }

    [Fact]
    public async Task Test_Login_WithInvalidPassword_ThrowsUnauthorized()
    {
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = loginDto.Email,
            PasswordHash = AuthService.HashPassword("correctpassword")
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);

        var act = async () => await _authService.Login(loginDto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Test_GetCurrentUser_WithValidId_ReturnsUserDto()
    {
        var userId = 1;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com"
        };
        var userDto = new UserDto
        {
            Id = userId,
            Username = user.Username,
            Email = user.Email
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

        var result = await _authService.GetCurrentUser(userId);

        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task Test_GetCurrentUser_WithInvalidId_ThrowsKeyNotFoundException()
    {
        var userId = 999;

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var act = async () => await _authService.GetCurrentUser(userId);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Test_UpdateCurrentUser_WithValidData_UpdatesUser()
    {
        var userId = 1;
        var updateDto = new UpdateUserDto
        {
            Username = "updateduser",
            Email = "updated@example.com",
            Avatar = "new-avatar-url"
        };
        var user = new User
        {
            Id = userId,
            Username = "originaluser",
            Email = "original@example.com",
            Avatar = "old-avatar"
        };
        var userDto = new UserDto
        {
            Id = userId,
            Username = updateDto.Username,
            Email = updateDto.Email,
            Avatar = updateDto.Avatar
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.GetByUsernameAsync(updateDto.Username!)).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(updateDto.Email!)).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

        var result = await _authService.UpdateCurrentUser(userId, updateDto);

        result.Should().NotBeNull();
        result.Username.Should().Be(updateDto.Username);
        result.Email.Should().Be(updateDto.Email);
        result.Avatar.Should().Be(updateDto.Avatar);
        _mockUserRepository.Verify(r => r.UpdateAsync(user), Times.Once);
    }
}
