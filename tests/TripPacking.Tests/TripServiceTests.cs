using AutoMapper;
using FluentAssertions;
using Moq;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;
using TripPacking.Services;
using Xunit;

namespace TripPacking.Tests;

public class TripServiceTests
{
    private readonly Mock<ITripRepository> _mockTripRepository;
    private readonly Mock<ITripMemberRepository> _mockTripMemberRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TripService _tripService;

    public TripServiceTests()
    {
        _mockTripRepository = new Mock<ITripRepository>();
        _mockTripMemberRepository = new Mock<ITripMemberRepository>();
        _mockMapper = new Mock<IMapper>();
        _tripService = new TripService(_mockTripRepository.Object, _mockTripMemberRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Test_CreateTrip_WithValidData_CreatesTripAndOwnerMember()
    {
        var ownerId = 1;
        var createDto = new CreateTripDto
        {
            Title = "Summer Vacation",
            Destination = "Hawaii",
            StartDate = new DateTime(2025, 7, 1),
            EndDate = new DateTime(2025, 7, 15)
        };
        var trip = new Trip
        {
            Id = 1,
            OwnerId = ownerId,
            Title = createDto.Title,
            Destination = createDto.Destination,
            StartDate = createDto.StartDate,
            EndDate = createDto.EndDate,
            Status = TripStatus.Planning
        };
        var tripDto = new TripDto
        {
            Id = trip.Id,
            OwnerId = trip.OwnerId,
            Title = trip.Title,
            Destination = trip.Destination,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Status = (int)trip.Status
        };

        _mockTripRepository.Setup(r => r.AddAsync(It.IsAny<Trip>())).Callback<Trip>(t => t.Id = 1).Returns(Task.CompletedTask);
        _mockTripMemberRepository.Setup(r => r.AddAsync(It.IsAny<TripMember>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<TripDto>(It.IsAny<Trip>())).Returns(tripDto);

        var result = await _tripService.Create(createDto, ownerId);

        result.Should().NotBeNull();
        result.Title.Should().Be(createDto.Title);
        result.OwnerId.Should().Be(ownerId);
        _mockTripRepository.Verify(r => r.AddAsync(It.IsAny<Trip>()), Times.Once);
        _mockTripMemberRepository.Verify(r => r.AddAsync(It.IsAny<TripMember>()), Times.Once);
    }

    [Fact]
    public async Task Test_GetById_WithNoAccess_ThrowsUnauthorized()
    {
        var tripId = 1;
        var currentUserId = 99;
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = 1,
            Title = "Test Trip"
        };
        var members = new List<TripMember>();

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);

        var act = async () => await _tripService.GetById(tripId, currentUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Test_GetById_WithOwnerAccess_ReturnsTripDto()
    {
        var tripId = 1;
        var currentUserId = 1;
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = currentUserId,
            Title = "Owner's Trip",
            Destination = "Paris",
            StartDate = new DateTime(2025, 6, 1),
            EndDate = new DateTime(2025, 6, 10),
            Status = TripStatus.Planning
        };
        var tripDto = new TripDto
        {
            Id = trip.Id,
            OwnerId = trip.OwnerId,
            Title = trip.Title,
            Destination = trip.Destination,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Status = (int)trip.Status
        };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockMapper.Setup(m => m.Map<TripDto>(trip)).Returns(tripDto);

        var result = await _tripService.GetById(tripId, currentUserId);

        result.Should().NotBeNull();
        result.Id.Should().Be(tripId);
        result.Title.Should().Be(trip.Title);
        result.OwnerId.Should().Be(currentUserId);
    }

    [Fact]
    public async Task Test_Delete_WithNonOwner_ThrowsUnauthorized()
    {
        var tripId = 1;
        var currentUserId = 99;
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = 1
        };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var act = async () => await _tripService.Delete(tripId, currentUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Test_Delete_WithOwner_DeletesTrip()
    {
        var tripId = 1;
        var currentUserId = 1;
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = currentUserId
        };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripRepository.Setup(r => r.DeleteAsync(trip)).Returns(Task.CompletedTask);

        var act = async () => await _tripService.Delete(tripId, currentUserId);

        await act.Should().NotThrowAsync();
        _mockTripRepository.Verify(r => r.DeleteAsync(trip), Times.Once);
    }
}
