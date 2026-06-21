using FluentAssertions;
using TripPacking.DTOs;
using Xunit;

namespace TripPacking.Tests;

public class ApiResponseTests
{
    [Fact]
    public void Test_Success_ReturnsCorrectCodeAndData()
    {
        var testData = "test-data";
        var message = "operation successful";

        var result = ApiResponse<string>.Success(testData, message);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Message.Should().Be(message);
        result.Data.Should().Be(testData);
    }

    [Fact]
    public void Test_Fail_ReturnsCorrectCodeAndMessage()
    {
        var errorMessage = "something went wrong";
        var errorCode = 500;

        var result = ApiResponse<string>.Fail(errorMessage, errorCode);

        result.Should().NotBeNull();
        result.Code.Should().Be(errorCode);
        result.Message.Should().Be(errorMessage);
        result.Data.Should().BeNull();
    }
}
