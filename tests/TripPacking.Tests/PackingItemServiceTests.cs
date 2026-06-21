using AutoMapper;
using FluentAssertions;
using Moq;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;
using TripPacking.Services;
using Xunit;

namespace TripPacking.Tests;

public class PackingItemServiceTests
{
    private readonly Mock<IPackingItemRepository> _mockPackingItemRepository;
    private readonly Mock<ITripRepository> _mockTripRepository;
    private readonly Mock<ITripMemberRepository> _mockTripMemberRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PackingItemService _packingItemService;

    public PackingItemServiceTests()
    {
        _mockPackingItemRepository = new Mock<IPackingItemRepository>();
        _mockTripRepository = new Mock<ITripRepository>();
        _mockTripMemberRepository = new Mock<ITripMemberRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _packingItemService = new PackingItemService(
            _mockPackingItemRepository.Object,
            _mockTripRepository.Object,
            _mockTripMemberRepository.Object,
            _mockUserRepository.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task Test_CreateItem_WithTripMemberAccess_CreatesItem()
    {
        var currentUserId = 1;
        var tripId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = 1,
            Name = "Sunscreen",
            Quantity = 2,
            IsPacked = false,
            IsShared = true
        };
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = currentUserId
        };
        var item = new PackingItem
        {
            Id = 1,
            TripId = tripId,
            CategoryId = createDto.CategoryId,
            Name = createDto.Name,
            Quantity = createDto.Quantity,
            IsPacked = createDto.IsPacked,
            IsShared = createDto.IsShared
        };
        var itemDto = new PackingItemDto
        {
            Id = item.Id,
            TripId = item.TripId,
            CategoryId = item.CategoryId,
            Name = item.Name,
            Quantity = item.Quantity,
            IsPacked = item.IsPacked,
            IsShared = item.IsShared
        };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingItemRepository.Setup(r => r.AddAsync(It.IsAny<PackingItem>())).Callback<PackingItem>(i => i.Id = 1).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<PackingItemDto>(It.IsAny<PackingItem>())).Returns(itemDto);

        var result = await _packingItemService.Create(createDto, currentUserId);

        result.Should().NotBeNull();
        result.Name.Should().Be(createDto.Name);
        result.Quantity.Should().Be(createDto.Quantity);
        _mockPackingItemRepository.Verify(r => r.AddAsync(It.IsAny<PackingItem>()), Times.Once);
    }

    [Fact]
    public async Task Test_CreateItem_WithNoAccess_ThrowsUnauthorized()
    {
        var currentUserId = 99;
        var tripId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = 1,
            Name = "Sunscreen",
            Quantity = 2
        };
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = 1
        };
        var members = new List<TripMember>();

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);

        var act = async () => await _packingItemService.Create(createDto, currentUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Test_UpdateItem_AsAssignedUser_UpdatesItem()
    {
        var currentUserId = 2;
        var itemId = 1;
        var tripId = 1;
        var updateDto = new UpdatePackingItemDto
        {
            Name = "Updated Sunscreen",
            Quantity = 3,
            IsPacked = true
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Original",
            Quantity = 1,
            AssignedTo = currentUserId,
            IsPacked = false,
            IsShared = false
        };
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = 99
        };
        var itemDto = new PackingItemDto
        {
            Id = item.Id,
            TripId = item.TripId,
            CategoryId = item.CategoryId,
            Name = updateDto.Name,
            Quantity = updateDto.Quantity.Value,
            IsPacked = updateDto.IsPacked.Value
        };

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingItemRepository.Setup(r => r.UpdateAsync(item)).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<PackingItemDto>(item)).Returns(itemDto);

        var result = await _packingItemService.Update(itemId, updateDto, currentUserId);

        result.Should().NotBeNull();
        result.Name.Should().Be(updateDto.Name);
        result.Quantity.Should().Be(updateDto.Quantity);
        result.IsPacked.Should().Be(updateDto.IsPacked.Value);
        _mockPackingItemRepository.Verify(r => r.UpdateAsync(item), Times.Once);
    }

    [Fact]
    public async Task Test_DeleteItem_AsNonOwner_ThrowsUnauthorized()
    {
        var currentUserId = 99;
        var itemId = 1;
        var tripId = 1;
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId
        };
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = 1
        };

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var act = async () => await _packingItemService.Delete(itemId, currentUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
