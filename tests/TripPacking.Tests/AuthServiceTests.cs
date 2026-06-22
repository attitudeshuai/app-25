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
    private readonly IPasswordHasher _passwordHasher;
    private readonly Mock<IMapper> _mockMapper;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _passwordHasher = new PasswordHasher();
        _mockMapper = new Mock<IMapper>();
        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockJwtService.Object,
            _passwordHasher,
            _mockMapper.Object);
    }

    [Fact]
    public async Task Test_Register_WithValidData_ReturnsUserDto_AndUsesBCrypt()
    {
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123"
        };
        var userDto = new UserDto
        {
            Id = 1,
            Username = registerDto.Username,
            Email = registerDto.Email
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(registerDto.Email)).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.GetByUsernameAsync(registerDto.Username)).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(userDto);

        var result = await _authService.Register(registerDto);

        result.Should().NotBeNull();
        result.Username.Should().Be(registerDto.Username);
        result.Email.Should().Be(registerDto.Email);
        _mockUserRepository.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.PasswordHashVersion == PasswordHashVersion.BCrypt &&
            u.PasswordHash != registerDto.Password)), Times.Once);
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
    public async Task Test_Login_WithValidBCryptCredentials_ReturnsAuthResult_NoResetNeeded()
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
            PasswordHash = _passwordHasher.HashPassword(loginDto.Password, PasswordHashVersion.BCrypt),
            PasswordHashVersion = PasswordHashVersion.BCrypt
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
        result.PasswordNeedsReset.Should().BeFalse();
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Test_Login_WithValidSha256Credentials_UpgradesToBCrypt_ReturnsResetFlag()
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
            PasswordHash = _passwordHasher.HashPassword(loginDto.Password, PasswordHashVersion.Sha256),
            PasswordHashVersion = PasswordHashVersion.Sha256
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
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var result = await _authService.Login(loginDto);

        result.Should().NotBeNull();
        result.Token.Should().Be(token);
        result.PasswordNeedsReset.Should().BeTrue();
        _mockUserRepository.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.PasswordHashVersion == PasswordHashVersion.BCrypt &&
            u.PasswordHash != _passwordHasher.HashPassword(loginDto.Password, PasswordHashVersion.Sha256))), Times.Once);
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
            PasswordHash = _passwordHasher.HashPassword("correctpassword", PasswordHashVersion.BCrypt),
            PasswordHashVersion = PasswordHashVersion.BCrypt
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);

        var act = async () => await _authService.Login(loginDto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Test_Login_WithInvalidSha256Password_ThrowsUnauthorized()
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
            PasswordHash = _passwordHasher.HashPassword("correctpassword", PasswordHashVersion.Sha256),
            PasswordHashVersion = PasswordHashVersion.Sha256
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

    [Fact]
    public async Task Test_ChangePassword_WithValidCurrentPassword_UpdatesPasswordToBCrypt()
    {
        var userId = 1;
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123"
        };
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = _passwordHasher.HashPassword(changePasswordDto.CurrentPassword, PasswordHashVersion.Sha256),
            PasswordHashVersion = PasswordHashVersion.Sha256
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var act = async () => await _authService.ChangePassword(userId, changePasswordDto);

        await act.Should().NotThrowAsync();
        _mockUserRepository.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.PasswordHashVersion == PasswordHashVersion.BCrypt &&
            _passwordHasher.VerifyPassword(changePasswordDto.NewPassword, u.PasswordHash, PasswordHashVersion.BCrypt))), Times.Once);
    }

    [Fact]
    public async Task Test_ChangePassword_WithInvalidCurrentPassword_ThrowsUnauthorized()
    {
        var userId = 1;
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "wrongpassword",
            NewPassword = "newpassword123"
        };
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = _passwordHasher.HashPassword("correctpassword", PasswordHashVersion.BCrypt),
            PasswordHashVersion = PasswordHashVersion.BCrypt
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var act = async () => await _authService.ChangePassword(userId, changePasswordDto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public void Test_PasswordHasher_BCrypt_HashAndVerify()
    {
        var password = "testpassword123";
        var hasher = new PasswordHasher();

        var hash = hasher.HashPassword(password, PasswordHashVersion.BCrypt);

        hash.Should().NotBe(password);
        hash.Should().StartWith("$2a$12$");
        hasher.VerifyPassword(password, hash, PasswordHashVersion.BCrypt).Should().BeTrue();
        hasher.VerifyPassword("wrongpassword", hash, PasswordHashVersion.BCrypt).Should().BeFalse();
    }

    [Fact]
    public void Test_PasswordHasher_Sha256_HashAndVerify()
    {
        var password = "testpassword123";
        var hasher = new PasswordHasher();

        var hash = hasher.HashPassword(password, PasswordHashVersion.Sha256);

        hash.Should().NotBe(password);
        hasher.VerifyPassword(password, hash, PasswordHashVersion.Sha256).Should().BeTrue();
        hasher.VerifyPassword("wrongpassword", hash, PasswordHashVersion.Sha256).Should().BeFalse();
    }

    [Fact]
    public void Test_PasswordHasher_GetCurrentVersion_ReturnsBCrypt()
    {
        var hasher = new PasswordHasher();

        hasher.GetCurrentVersion().Should().Be(PasswordHashVersion.BCrypt);
    }

    [Fact]
    public void Test_PasswordHasher_BCrypt_ProducesDifferentHashesForSamePassword()
    {
        var password = "samepassword";
        var hasher = new PasswordHasher();

        var hash1 = hasher.HashPassword(password, PasswordHashVersion.BCrypt);
        var hash2 = hasher.HashPassword(password, PasswordHashVersion.BCrypt);

        hash1.Should().NotBe(hash2);
        hasher.VerifyPassword(password, hash1, PasswordHashVersion.BCrypt).Should().BeTrue();
        hasher.VerifyPassword(password, hash2, PasswordHashVersion.BCrypt).Should().BeTrue();
    }

    [Fact]
    public void Test_PasswordHasher_Sha256_ProducesSameHashForSamePassword()
    {
        var password = "samepassword";
        var hasher = new PasswordHasher();

        var hash1 = hasher.HashPassword(password, PasswordHashVersion.Sha256);
        var hash2 = hasher.HashPassword(password, PasswordHashVersion.Sha256);

        hash1.Should().Be(hash2);
    }
}
