using FluentAssertions;
using Microsoft.Extensions.Options;
using TripPacking.Config;
using TripPacking.Services;
using Xunit;

namespace TripPacking.Tests;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        var jwtSettings = new JwtSettings
        {
            SecretKey = "this_is_a_very_long_secret_key_for_testing_purposes_12345",
            Issuer = "TripPackingTest",
            Audience = "TripPackingAudience",
            ExpirationMinutes = 60
        };
        var options = Options.Create(jwtSettings);
        _jwtService = new JwtService(options);
    }

    [Fact]
    public void Test_GenerateToken_WithValidData_ReturnsNonEmptyToken()
    {
        var userId = 1;
        var username = "testuser";
        var email = "test@example.com";

        var token = _jwtService.GenerateToken(userId, username, email);

        token.Should().NotBeNullOrEmpty();
        token.Should().Contain(".");
    }
}
