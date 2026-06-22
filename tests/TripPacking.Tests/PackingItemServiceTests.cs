using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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
    private readonly Mock<IPackingCategoryRepository> _mockPackingCategoryRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<PackingItemService>> _mockLogger;
    private readonly PackingItemService _packingItemService;

    public PackingItemServiceTests()
    {
        _mockPackingItemRepository = new Mock<IPackingItemRepository>();
        _mockTripRepository = new Mock<ITripRepository>();
        _mockTripMemberRepository = new Mock<ITripMemberRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPackingCategoryRepository = new Mock<IPackingCategoryRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PackingItemService>>();
        _packingItemService = new PackingItemService(
            _mockPackingItemRepository.Object,
            _mockTripRepository.Object,
            _mockTripMemberRepository.Object,
            _mockUserRepository.Object,
            _mockPackingCategoryRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    private Trip CreateValidTrip(int tripId, int ownerId)
    {
        return new Trip
        {
            Id = tripId,
            OwnerId = ownerId,
            StartDate = new DateTime(2025, 6, 1),
            EndDate = new DateTime(2025, 6, 5)
        };
    }

    private PackingCategory CreateValidCategory(int categoryId, int tripId)
    {
        return new PackingCategory
        {
            Id = categoryId,
            TripId = tripId,
            Name = "Clothing"
        };
    }

    [Fact]
    public async Task Test_CreateItem_WithTripMemberAccess_CreatesItem()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = 2,
            IsPacked = false,
            IsShared = true
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);
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
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
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
        var trip = CreateValidTrip(tripId, 1);
        var members = new List<TripMember>();

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);

        var act = async () => await _packingItemService.Create(createDto, currentUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Test_CreateItem_WithCategoryNotBelongingToTrip_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var tripId = 1;
        var otherTripId = 2;
        var categoryId = 10;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = 2
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, otherTripId);

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);

        var act = async () => await _packingItemService.Create(createDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("不属于当前旅行");
    }

    [Fact]
    public async Task Test_CreateItem_WithNonExistentCategory_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 999;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = 2
        };
        var trip = CreateValidTrip(tripId, currentUserId);

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync((PackingCategory?)null);

        var act = async () => await _packingItemService.Create(createDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("分类不存在");
    }

    [Fact]
    public async Task Test_CreateItem_WithAssignedUserNotTripMember_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var nonMemberUserId = 50;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = 2,
            AssignedTo = nonMemberUserId
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);
        var members = new List<TripMember>();

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);

        var act = async () => await _packingItemService.Create(createDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("不是当前旅行的成员");
    }

    [Fact]
    public async Task Test_CreateItem_WithAssignedTripOwner_Succeeds()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = 2,
            AssignedTo = currentUserId
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);
        var itemDto = new PackingItemDto { Id = 1, Name = createDto.Name };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _mockPackingItemRepository.Setup(r => r.AddAsync(It.IsAny<PackingItem>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<PackingItemDto>(It.IsAny<PackingItem>())).Returns(itemDto);

        var result = await _packingItemService.Create(createDto, currentUserId);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_CreateItem_WithAssignedTripMember_Succeeds()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var memberUserId = 5;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = 2,
            AssignedTo = memberUserId
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);
        var members = new List<TripMember>
        {
            new TripMember { TripId = tripId, UserId = memberUserId }
        };
        var itemDto = new PackingItemDto { Id = 1, Name = createDto.Name };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);
        _mockPackingItemRepository.Setup(r => r.AddAsync(It.IsAny<PackingItem>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<PackingItemDto>(It.IsAny<PackingItem>())).Returns(itemDto);

        var result = await _packingItemService.Create(createDto, currentUserId);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_CreateItem_WithZeroQuantity_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = 0
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);

        var act = async () => await _packingItemService.Create(createDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("数量必须大于等于 1");
    }

    [Fact]
    public async Task Test_CreateItem_WithNegativeQuantity_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = -5
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);

        var act = async () => await _packingItemService.Create(createDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("数量必须大于等于 1");
    }

    [Fact]
    public async Task Test_CreateItem_WithNegativeDayNumber_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = 1,
            DayNumber = -1
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);

        var act = async () => await _packingItemService.Create(createDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("天数必须大于等于 1");
    }

    [Fact]
    public async Task Test_CreateItem_WithDayNumberExceedingTripLength_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = 1,
            DayNumber = 10
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);

        var act = async () => await _packingItemService.Create(createDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("不能超过行程总天数 5 天");
    }

    [Fact]
    public async Task Test_CreateItem_WithValidDayNumber_Succeeds()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = 1,
            DayNumber = 3
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);
        var itemDto = new PackingItemDto { Id = 1, Name = createDto.Name, DayNumber = 3 };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _mockPackingItemRepository.Setup(r => r.AddAsync(It.IsAny<PackingItem>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<PackingItemDto>(It.IsAny<PackingItem>())).Returns(itemDto);

        var result = await _packingItemService.Create(createDto, currentUserId);

        result.Should().NotBeNull();
        result.DayNumber.Should().Be(3);
    }

    [Fact]
    public async Task Test_CreateItem_WithDayNumberEqualToLastDay_Succeeds()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "Sunscreen",
            Quantity = 1,
            DayNumber = 5
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);
        var itemDto = new PackingItemDto { Id = 1, Name = createDto.Name, DayNumber = 5 };

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _mockPackingItemRepository.Setup(r => r.AddAsync(It.IsAny<PackingItem>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<PackingItemDto>(It.IsAny<PackingItem>())).Returns(itemDto);

        var result = await _packingItemService.Create(createDto, currentUserId);

        result.Should().NotBeNull();
        result.DayNumber.Should().Be(5);
    }

    [Fact]
    public async Task Test_CreateItem_WithEmptyName_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "",
            Quantity = 1
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);

        var act = async () => await _packingItemService.Create(createDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("物品名称不能为空");
    }

    [Fact]
    public async Task Test_CreateItem_WithWhitespaceName_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var tripId = 1;
        var categoryId = 1;
        var createDto = new CreatePackingItemDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            Name = "   ",
            Quantity = 1
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(categoryId, tripId);

        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);

        var act = async () => await _packingItemService.Create(createDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("物品名称不能为空");
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
        var trip = CreateValidTrip(tripId, 99);
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
    public async Task Test_UpdateItem_WithCategoryNotBelongingToTrip_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var itemId = 1;
        var tripId = 1;
        var otherTripId = 2;
        var newCategoryId = 99;
        var updateDto = new UpdatePackingItemDto
        {
            CategoryId = newCategoryId
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Original",
            Quantity = 1
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var category = CreateValidCategory(newCategoryId, otherTripId);

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockPackingCategoryRepository.Setup(r => r.GetByIdAsync(newCategoryId)).ReturnsAsync(category);

        var act = async () => await _packingItemService.Update(itemId, updateDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("不属于当前旅行");
    }

    [Fact]
    public async Task Test_UpdateItem_WithNegativeQuantity_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var itemId = 1;
        var tripId = 1;
        var updateDto = new UpdatePackingItemDto
        {
            Quantity = -10
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Original",
            Quantity = 1,
            AssignedTo = currentUserId
        };
        var trip = CreateValidTrip(tripId, 99);

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var act = async () => await _packingItemService.Update(itemId, updateDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("数量必须大于等于 1");
    }

    [Fact]
    public async Task Test_UpdateItem_WithZeroQuantity_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var itemId = 1;
        var tripId = 1;
        var updateDto = new UpdatePackingItemDto
        {
            Quantity = 0
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Original",
            Quantity = 1,
            AssignedTo = currentUserId
        };
        var trip = CreateValidTrip(tripId, 99);

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var act = async () => await _packingItemService.Update(itemId, updateDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("数量必须大于等于 1");
    }

    [Fact]
    public async Task Test_UpdateItem_WithDayNumberExceedingTripLength_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var itemId = 1;
        var tripId = 1;
        var updateDto = new UpdatePackingItemDto
        {
            DayNumber = 999
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Original",
            Quantity = 1,
            AssignedTo = currentUserId
        };
        var trip = CreateValidTrip(tripId, 99);

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var act = async () => await _packingItemService.Update(itemId, updateDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("不能超过行程总天数 5 天");
    }

    [Fact]
    public async Task Test_UpdateItem_WithAssignedUserNotTripMember_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var itemId = 1;
        var tripId = 1;
        var nonMemberUserId = 50;
        var updateDto = new UpdatePackingItemDto
        {
            AssignedTo = nonMemberUserId
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Original",
            Quantity = 1,
            AssignedTo = currentUserId
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var members = new List<TripMember>();

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);

        var act = async () => await _packingItemService.Update(itemId, updateDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("不是当前旅行的成员");
    }

    [Fact]
    public async Task Test_UpdateItem_WithEmptyName_ThrowsArgumentException()
    {
        var currentUserId = 1;
        var itemId = 1;
        var tripId = 1;
        var updateDto = new UpdatePackingItemDto
        {
            Name = ""
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Original",
            Quantity = 1,
            AssignedTo = currentUserId
        };
        var trip = CreateValidTrip(tripId, 99);

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var act = async () => await _packingItemService.Update(itemId, updateDto, currentUserId);

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.And.Message.Should().Contain("物品名称不能为空");
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

    [Fact]
    public async Task Test_UpdateSharedItemPackingStatus_AsTripMember_Succeeds()
    {
        var currentUserId = 5;
        var itemId = 1;
        var tripId = 1;
        var updateDto = new UpdatePackingItemDto
        {
            IsPacked = true
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Shared Tent",
            Quantity = 1,
            IsPacked = false,
            IsShared = true,
            AssignedTo = 99
        };
        var trip = CreateValidTrip(tripId, 1);
        var members = new List<TripMember>
        {
            new TripMember { TripId = tripId, UserId = currentUserId }
        };
        var user = new User { Id = currentUserId, Username = "member01" };
        var itemDto = new PackingItemDto
        {
            Id = item.Id,
            IsPacked = true
        };

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);
        _mockUserRepository.Setup(r => r.GetByIdAsync(currentUserId)).ReturnsAsync(user);
        _mockPackingItemRepository.Setup(r => r.UpdateAsync(item)).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<PackingItemDto>(item)).Returns(itemDto);

        var result = await _packingItemService.Update(itemId, updateDto, currentUserId);

        result.Should().NotBeNull();
        result.IsPacked.Should().BeTrue();
        _mockPackingItemRepository.Verify(r => r.UpdateAsync(item), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Test_UpdateSharedItemPackingStatus_AsNonTripMember_ThrowsUnauthorized()
    {
        var currentUserId = 99;
        var itemId = 1;
        var tripId = 1;
        var updateDto = new UpdatePackingItemDto
        {
            IsPacked = true
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Shared Tent",
            Quantity = 1,
            IsPacked = false,
            IsShared = true,
            AssignedTo = 1
        };
        var trip = CreateValidTrip(tripId, 1);
        var members = new List<TripMember>();

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);

        var act = async () => await _packingItemService.Update(itemId, updateDto, currentUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Test_UpdateSharedItemPackingStatus_WithOtherFields_ThrowsUnauthorized()
    {
        var currentUserId = 5;
        var itemId = 1;
        var tripId = 1;
        var updateDto = new UpdatePackingItemDto
        {
            IsPacked = true,
            Name = "Updated Name"
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Shared Tent",
            Quantity = 1,
            IsPacked = false,
            IsShared = true,
            AssignedTo = 99
        };
        var trip = CreateValidTrip(tripId, 1);
        var members = new List<TripMember>
        {
            new TripMember { TripId = tripId, UserId = currentUserId }
        };

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);

        var act = async () => await _packingItemService.Update(itemId, updateDto, currentUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Test_UpdateNonSharedItemPackingStatus_AsNonAssignedMember_ThrowsUnauthorized()
    {
        var currentUserId = 5;
        var itemId = 1;
        var tripId = 1;
        var updateDto = new UpdatePackingItemDto
        {
            IsPacked = true
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Personal Item",
            Quantity = 1,
            IsPacked = false,
            IsShared = false,
            AssignedTo = 99
        };
        var trip = CreateValidTrip(tripId, 1);
        var members = new List<TripMember>
        {
            new TripMember { TripId = tripId, UserId = currentUserId }
        };

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);

        var act = async () => await _packingItemService.Update(itemId, updateDto, currentUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Test_UpdateSharedItem_AsOwner_CanUpdateAllFields()
    {
        var currentUserId = 1;
        var itemId = 1;
        var tripId = 1;
        var updateDto = new UpdatePackingItemDto
        {
            Name = "Updated Shared Tent",
            Quantity = 2,
            IsPacked = true
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Shared Tent",
            Quantity = 1,
            IsPacked = false,
            IsShared = true,
            AssignedTo = 2
        };
        var trip = CreateValidTrip(tripId, currentUserId);
        var itemDto = new PackingItemDto
        {
            Id = item.Id,
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
        result.IsPacked.Should().BeTrue();
        _mockPackingItemRepository.Verify(r => r.UpdateAsync(item), Times.Once);
    }

    [Fact]
    public async Task Test_UpdateSharedItemPackingStatus_NoStatusChange_DoesNotLog()
    {
        var currentUserId = 5;
        var itemId = 1;
        var tripId = 1;
        var updateDto = new UpdatePackingItemDto
        {
            IsPacked = true
        };
        var item = new PackingItem
        {
            Id = itemId,
            TripId = tripId,
            CategoryId = 1,
            Name = "Shared Tent",
            Quantity = 1,
            IsPacked = true,
            IsShared = true,
            AssignedTo = 99
        };
        var trip = CreateValidTrip(tripId, 1);
        var members = new List<TripMember>
        {
            new TripMember { TripId = tripId, UserId = currentUserId }
        };
        var itemDto = new PackingItemDto
        {
            Id = item.Id,
            IsPacked = true
        };

        _mockPackingItemRepository.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _mockTripRepository.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        _mockTripMemberRepository.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(members);
        _mockPackingItemRepository.Setup(r => r.UpdateAsync(item)).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<PackingItemDto>(item)).Returns(itemDto);

        var result = await _packingItemService.Update(itemId, updateDto, currentUserId);

        result.Should().NotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }
}
