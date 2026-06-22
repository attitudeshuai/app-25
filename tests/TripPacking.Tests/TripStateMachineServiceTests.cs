using AutoMapper;
using FluentAssertions;
using Moq;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;
using TripPacking.Services;
using Xunit;

namespace TripPacking.Tests;

public class TripStateMachineServiceTests
{
    private readonly Mock<ITripRepository> _mockTripRepository;
    private readonly Mock<IPackingItemRepository> _mockPackingItemRepository;
    private readonly Mock<ITripMemberRepository> _mockTripMemberRepository;
    private readonly Mock<ITripStatusHistoryRepository> _mockStatusHistoryRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TripStateMachineService _stateMachineService;

    public TripStateMachineServiceTests()
    {
        _mockTripRepository = new Mock<ITripRepository>();
        _mockPackingItemRepository = new Mock<IPackingItemRepository>();
        _mockTripMemberRepository = new Mock<ITripMemberRepository>();
        _mockStatusHistoryRepository = new Mock<ITripStatusHistoryRepository>();
        _mockMapper = new Mock<IMapper>();
        _stateMachineService = new TripStateMachineService(
            _mockTripRepository.Object,
            _mockPackingItemRepository.Object,
            _mockTripMemberRepository.Object,
            _mockStatusHistoryRepository.Object,
            _mockMapper.Object);
    }

    #region CanTransition Tests

    [Theory]
    [InlineData(TripStatus.Planning, TripStatus.Ongoing, true)]
    [InlineData(TripStatus.Planning, TripStatus.Completed, false)]
    [InlineData(TripStatus.Ongoing, TripStatus.Completed, true)]
    [InlineData(TripStatus.Ongoing, TripStatus.Planning, true)]
    [InlineData(TripStatus.Completed, TripStatus.Ongoing, true)]
    [InlineData(TripStatus.Completed, TripStatus.Planning, false)]
    [InlineData(TripStatus.Planning, TripStatus.Planning, false)]
    [InlineData(TripStatus.Ongoing, TripStatus.Ongoing, false)]
    [InlineData(TripStatus.Completed, TripStatus.Completed, false)]
    public void CanTransition_WithVariousTransitions_ReturnsExpectedResult(
        TripStatus from, TripStatus to, bool expected)
    {
        var result = _stateMachineService.CanTransition(from, to);
        result.Should().Be(expected);
    }

    #endregion

    #region TransitionStatusAsync - Forward Transitions

    [Fact]
    public async Task TransitionStatusAsync_PlanningToOngoing_AsOwner_Succeeds()
    {
        var tripId = 1;
        var ownerId = 1;
        var trip = new Trip { Id = tripId, OwnerId = ownerId, Status = TripStatus.Planning };
        var tripDto = new TripDto { Id = tripId, Status = (int)TripStatus.Ongoing };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripRepository.Setup(r => r.UpdateAsync(It.IsAny<Trip>())).Returns(Task.CompletedTask);
        _mockStatusHistoryRepository.Setup(r => r.AddAsync(It.IsAny<TripStatusHistory>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<TripDto>(It.IsAny<Trip>())).Returns(tripDto);

        var result = await _stateMachineService.TransitionStatusAsync(
            tripId, TripStatus.Ongoing, ownerId, null);

        result.Success.Should().BeTrue();
        result.Trip.Should().NotBeNull();
        result.Trip!.Status.Should().Be((int)TripStatus.Ongoing);
        _mockStatusHistoryRepository.Verify(r => r.AddAsync(It.IsAny<TripStatusHistory>()), Times.Once);
        _mockTripRepository.Verify(r => r.UpdateAsync(It.Is<Trip>(t => t.Status == TripStatus.Ongoing)), Times.Once);
    }

    [Fact]
    public async Task TransitionStatusAsync_OngoingToCompleted_AllPacked_Succeeds()
    {
        var tripId = 1;
        var ownerId = 1;
        var trip = new Trip { Id = tripId, OwnerId = ownerId, Status = TripStatus.Ongoing };
        var packedItems = new List<PackingItem>
        {
            new() { Id = 1, TripId = tripId, Name = "Passport", IsPacked = true },
            new() { Id = 2, TripId = tripId, Name = "Clothes", IsPacked = true }
        };
        var tripDto = new TripDto { Id = tripId, Status = (int)TripStatus.Completed };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingItemRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(packedItems);
        _mockTripRepository.Setup(r => r.UpdateAsync(It.IsAny<Trip>())).Returns(Task.CompletedTask);
        _mockStatusHistoryRepository.Setup(r => r.AddAsync(It.IsAny<TripStatusHistory>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<TripDto>(It.IsAny<Trip>())).Returns(tripDto);

        var result = await _stateMachineService.TransitionStatusAsync(
            tripId, TripStatus.Completed, ownerId, null);

        result.Success.Should().BeTrue();
        result.UnpackedItems.Should().BeNull();
        _mockStatusHistoryRepository.Verify(r => r.AddAsync(It.IsAny<TripStatusHistory>()), Times.Once);
    }

    #endregion

    #region TransitionStatusAsync - Packing Validation

    [Fact]
    public async Task TransitionStatusAsync_OngoingToCompleted_UnpackedItems_ReturnsUnpackedList()
    {
        var tripId = 1;
        var ownerId = 1;
        var trip = new Trip { Id = tripId, OwnerId = ownerId, Status = TripStatus.Ongoing };
        var items = new List<PackingItem>
        {
            new() { Id = 1, TripId = tripId, Name = "Passport", IsPacked = true },
            new() { Id = 2, TripId = tripId, Name = "Charger", IsPacked = false }
        };
        var unpackedDtos = new List<PackingItemDto>
        {
            new() { Id = 2, Name = "Charger", IsPacked = false }
        };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingItemRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(items);
        _mockMapper.Setup(m => m.Map<List<PackingItemDto>>(It.IsAny<List<PackingItem>>())).Returns(unpackedDtos);

        var result = await _stateMachineService.TransitionStatusAsync(
            tripId, TripStatus.Completed, ownerId, null);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not packed");
        result.UnpackedItems.Should().NotBeNull();
        result.UnpackedItems!.Should().HaveCount(1);
        result.UnpackedItems!.First().Name.Should().Be("Charger");
        _mockStatusHistoryRepository.Verify(r => r.AddAsync(It.IsAny<TripStatusHistory>()), Times.Never);
        _mockTripRepository.Verify(r => r.UpdateAsync(It.IsAny<Trip>()), Times.Never);
    }

    #endregion

    #region TransitionStatusAsync - Reverse Transitions

    [Fact]
    public async Task TransitionStatusAsync_OngoingToPlanning_NoReason_Fails()
    {
        var tripId = 1;
        var ownerId = 1;
        var trip = new Trip { Id = tripId, OwnerId = ownerId, Status = TripStatus.Ongoing };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var result = await _stateMachineService.TransitionStatusAsync(
            tripId, TripStatus.Planning, ownerId, null);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("reason");
        _mockStatusHistoryRepository.Verify(r => r.AddAsync(It.IsAny<TripStatusHistory>()), Times.Never);
        _mockTripRepository.Verify(r => r.UpdateAsync(It.IsAny<Trip>()), Times.Never);
    }

    [Fact]
    public async Task TransitionStatusAsync_OngoingToPlanning_WithReason_Succeeds()
    {
        var tripId = 1;
        var ownerId = 1;
        var reason = "Need to add more items";
        var trip = new Trip { Id = tripId, OwnerId = ownerId, Status = TripStatus.Ongoing };
        var tripDto = new TripDto { Id = tripId, Status = (int)TripStatus.Planning };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripRepository.Setup(r => r.UpdateAsync(It.IsAny<Trip>())).Returns(Task.CompletedTask);
        _mockStatusHistoryRepository.Setup(r => r.AddAsync(It.IsAny<TripStatusHistory>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<TripDto>(It.IsAny<Trip>())).Returns(tripDto);

        var result = await _stateMachineService.TransitionStatusAsync(
            tripId, TripStatus.Planning, ownerId, reason);

        result.Success.Should().BeTrue();
        _mockStatusHistoryRepository.Verify(r => r.AddAsync(
            It.Is<TripStatusHistory>(h => h.Reason == reason)), Times.Once);
    }

    [Fact]
    public async Task TransitionStatusAsync_CompletedToPlanning_InvalidTransition_Fails()
    {
        var tripId = 1;
        var ownerId = 1;
        var trip = new Trip { Id = tripId, OwnerId = ownerId, Status = TripStatus.Completed };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var result = await _stateMachineService.TransitionStatusAsync(
            tripId, TripStatus.Planning, ownerId, "Some reason");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid status transition");
        _mockStatusHistoryRepository.Verify(r => r.AddAsync(It.IsAny<TripStatusHistory>()), Times.Never);
    }

    #endregion

    #region TransitionStatusAsync - Authorization

    [Fact]
    public async Task TransitionStatusAsync_NonOwner_Fails()
    {
        var tripId = 1;
        var ownerId = 1;
        var nonOwnerId = 99;
        var trip = new Trip { Id = tripId, OwnerId = ownerId, Status = TripStatus.Planning };
        var members = new List<TripMember>();

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);

        var result = await _stateMachineService.TransitionStatusAsync(
            tripId, TripStatus.Ongoing, nonOwnerId, null);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Only trip owner");
        _mockStatusHistoryRepository.Verify(r => r.AddAsync(It.IsAny<TripStatusHistory>()), Times.Never);
    }

    [Fact]
    public async Task TransitionStatusAsync_TripNotFound_ReturnsError()
    {
        var tripId = 999;
        var userId = 1;

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync((Trip?)null);

        var result = await _stateMachineService.TransitionStatusAsync(
            tripId, TripStatus.Ongoing, userId, null);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    #endregion

    #region TransitionStatusAsync - Same Status

    [Fact]
    public async Task TransitionStatusAsync_SameStatus_ReturnsSuccessWithMessage()
    {
        var tripId = 1;
        var ownerId = 1;
        var trip = new Trip { Id = tripId, OwnerId = ownerId, Status = TripStatus.Planning };
        var tripDto = new TripDto { Id = tripId, Status = (int)TripStatus.Planning };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockMapper.Setup(m => m.Map<TripDto>(trip)).Returns(tripDto);

        var result = await _stateMachineService.TransitionStatusAsync(
            tripId, TripStatus.Planning, ownerId, null);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("already in target status");
        _mockStatusHistoryRepository.Verify(r => r.AddAsync(It.IsAny<TripStatusHistory>()), Times.Never);
        _mockTripRepository.Verify(r => r.UpdateAsync(It.IsAny<Trip>()), Times.Never);
    }

    #endregion

    #region GetStatusHistoryAsync

    [Fact]
    public async Task GetStatusHistoryAsync_AsOwner_ReturnsHistory()
    {
        var tripId = 1;
        var ownerId = 1;
        var trip = new Trip { Id = tripId, OwnerId = ownerId };
        var histories = new List<TripStatusHistory>
        {
            new()
            {
                Id = 1,
                TripId = tripId,
                FromStatus = TripStatus.Planning,
                ToStatus = TripStatus.Ongoing,
                ChangedBy = ownerId,
                ChangedAt = DateTime.UtcNow,
                ChangedByUser = new User { Id = ownerId, Username = "testuser" }
            }
        };
        var historyDtos = new List<TripStatusHistoryDto>
        {
            new()
            {
                Id = 1,
                FromStatus = (int)TripStatus.Planning,
                ToStatus = (int)TripStatus.Ongoing,
                ChangedBy = ownerId,
                ChangedByUserName = "testuser",
                ChangedAt = histories[0].ChangedAt
            }
        };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockStatusHistoryRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(histories);
        _mockMapper.Setup(m => m.Map<IEnumerable<TripStatusHistoryDto>>(histories)).Returns(historyDtos);

        var result = await _stateMachineService.GetStatusHistoryAsync(tripId, ownerId);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().ChangedByUserName.Should().Be("testuser");
    }

    [Fact]
    public async Task GetStatusHistoryAsync_NoAccess_ThrowsUnauthorized()
    {
        var tripId = 1;
        var ownerId = 1;
        var otherUserId = 99;
        var trip = new Trip { Id = tripId, OwnerId = ownerId };
        var members = new List<TripMember>();

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);

        var act = async () => await _stateMachineService.GetStatusHistoryAsync(tripId, otherUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetStatusHistoryAsync_TripNotFound_ThrowsKeyNotFound()
    {
        var tripId = 999;
        var userId = 1;

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync((Trip?)null);

        var act = async () => await _stateMachineService.GetStatusHistoryAsync(tripId, userId);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion
}
