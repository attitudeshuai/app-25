using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class PackingItemService : IPackingItemService
{
    private readonly IPackingItemRepository _packingItemRepository;
    private readonly ITripRepository _tripRepository;
    private readonly ITripMemberRepository _tripMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPackingCategoryRepository _packingCategoryRepository;
    private readonly IMapper _mapper;

    public PackingItemService(IPackingItemRepository packingItemRepository, ITripRepository tripRepository, ITripMemberRepository tripMemberRepository, IUserRepository userRepository, IPackingCategoryRepository packingCategoryRepository, IMapper mapper)
    {
        _packingItemRepository = packingItemRepository;
        _tripRepository = tripRepository;
        _tripMemberRepository = tripMemberRepository;
        _userRepository = userRepository;
        _packingCategoryRepository = packingCategoryRepository;
        _mapper = mapper;
    }

    private async Task<bool> HasTripAccess(int tripId, int userId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);
        if (trip == null)
            return false;

        if (trip.OwnerId == userId)
            return true;

        var members = await _tripMemberRepository.GetByTripIdAsync(tripId);
        return members.Any(m => m.UserId == userId);
    }

    private async Task<bool> IsTripOwner(int tripId, int userId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);
        return trip != null && trip.OwnerId == userId;
    }

    private async Task ValidateCategoryBelongsToTrip(int categoryId, int tripId)
    {
        var category = await _packingCategoryRepository.GetByIdAsync(categoryId);
        if (category == null)
            throw new ArgumentException($"分类不存在：CategoryId={categoryId}");

        if (category.TripId != tripId)
            throw new ArgumentException($"分类 CategoryId={categoryId} 不属于当前旅行 TripId={tripId}");
    }

    private async Task ValidateAssignedUserIsTripMember(int? assignedTo, int tripId)
    {
        if (!assignedTo.HasValue)
            return;

        var trip = await _tripRepository.GetByIdAsync(tripId);
        if (trip == null)
            throw new ArgumentException($"旅行不存在：TripId={tripId}");

        if (trip.OwnerId == assignedTo.Value)
            return;

        var members = await _tripMemberRepository.GetByTripIdAsync(tripId);
        if (!members.Any(m => m.UserId == assignedTo.Value))
            throw new ArgumentException($"负责人 UserId={assignedTo.Value} 不是当前旅行的成员");
    }

    private void ValidateDayNumberRange(int? dayNumber, Trip trip)
    {
        if (!dayNumber.HasValue)
            return;

        var totalDays = (int)(trip.EndDate.Date - trip.StartDate.Date).TotalDays + 1;
        if (dayNumber.Value < 1)
            throw new ArgumentException($"天数必须大于等于 1，当前值为 {dayNumber.Value}");

        if (dayNumber.Value > totalDays)
            throw new ArgumentException($"天数不能超过行程总天数 {totalDays} 天，当前值为 {dayNumber.Value}");
    }

    private void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException($"数量必须大于等于 1，当前值为 {quantity}");
    }

    private void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("物品名称不能为空或空白字符");

        if (name.Length > 200)
            throw new ArgumentException($"物品名称不能超过 200 个字符，当前长度为 {name.Length}");
    }

    private async Task<PackingItemDto> EnrichDto(PackingItem item)
    {
        var dto = _mapper.Map<PackingItemDto>(item);
        if (item.AssignedTo.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(item.AssignedTo.Value);
            if (user != null)
                dto.AssignedUsername = user.Username;
        }
        return dto;
    }

    public async Task<PagedResult<PackingItemDto>> GetPaged(PackingItemQueryDto query, int currentUserId)
    {
        if (query.TripId.HasValue && !await HasTripAccess(query.TripId.Value, currentUserId))
            throw new UnauthorizedAccessException("No access to this trip");

        var pagedResult = await _packingItemRepository.GetPagedAsync(query.PageIndex, query.PageSize, query.Keyword, query.TripId, query.CategoryId, query.IsPacked, query.IsShared);

        var items = pagedResult.Items;
        var total = pagedResult.Total;

        if (!query.TripId.HasValue)
        {
            var userMemberships = await _tripMemberRepository.GetByUserIdAsync(currentUserId);
            var ownedTrips = await _tripRepository.GetByOwnerIdAsync(currentUserId);
            var accessibleTripIds = userMemberships.Select(m => m.TripId).Concat(ownedTrips.Select(t => t.Id)).ToHashSet();
            items = items.Where(i => accessibleTripIds.Contains(i.TripId)).ToList();
            total = items.Count();
        }

        var dtos = new List<PackingItemDto>();
        foreach (var item in items)
            dtos.Add(await EnrichDto(item));

        return new PagedResult<PackingItemDto> { Items = dtos, Total = total };
    }

    public async Task<PackingItemDto> GetById(int id, int currentUserId)
    {
        var item = await _packingItemRepository.GetByIdAsync(id);
        if (item == null)
            throw new KeyNotFoundException("Packing item not found");

        if (!await HasTripAccess(item.TripId, currentUserId))
            throw new UnauthorizedAccessException("No access to this trip");

        return await EnrichDto(item);
    }

    public async Task<PackingItemDto> Create(CreatePackingItemDto dto, int currentUserId)
    {
        if (!await HasTripAccess(dto.TripId, currentUserId))
            throw new UnauthorizedAccessException("No access to create items for this trip");

        var trip = await _tripRepository.GetByIdAsync(dto.TripId);
        if (trip == null)
            throw new ArgumentException($"旅行不存在：TripId={dto.TripId}");

        ValidateName(dto.Name);
        ValidateQuantity(dto.Quantity);
        ValidateDayNumberRange(dto.DayNumber, trip);
        await ValidateCategoryBelongsToTrip(dto.CategoryId, dto.TripId);
        await ValidateAssignedUserIsTripMember(dto.AssignedTo, dto.TripId);

        var item = new PackingItem
        {
            TripId = dto.TripId,
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            Quantity = dto.Quantity,
            AssignedTo = dto.AssignedTo,
            IsPacked = dto.IsPacked,
            IsShared = dto.IsShared,
            DayNumber = dto.DayNumber,
            CreatedAt = DateTime.UtcNow
        };

        await _packingItemRepository.AddAsync(item);
        return await EnrichDto(item);
    }

    public async Task<PackingItemDto> Update(int id, UpdatePackingItemDto dto, int currentUserId)
    {
        var item = await _packingItemRepository.GetByIdAsync(id);
        if (item == null)
            throw new KeyNotFoundException("Packing item not found");

        var isOwner = await IsTripOwner(item.TripId, currentUserId);
        var isAssigned = item.AssignedTo.HasValue && item.AssignedTo.Value == currentUserId;

        if (!isOwner && !isAssigned)
            throw new UnauthorizedAccessException("Only trip owner or assigned user can update this item");

        var trip = await _tripRepository.GetByIdAsync(item.TripId);
        if (trip == null)
            throw new ArgumentException($"旅行不存在：TripId={item.TripId}");

        if (dto.CategoryId.HasValue)
        {
            await ValidateCategoryBelongsToTrip(dto.CategoryId.Value, item.TripId);
            item.CategoryId = dto.CategoryId.Value;
        }

        if (dto.Name != null)
        {
            ValidateName(dto.Name);
            item.Name = dto.Name;
        }

        if (dto.Quantity.HasValue)
        {
            ValidateQuantity(dto.Quantity.Value);
            item.Quantity = dto.Quantity.Value;
        }

        if (dto.AssignedTo.HasValue)
        {
            await ValidateAssignedUserIsTripMember(dto.AssignedTo.Value, item.TripId);
            item.AssignedTo = dto.AssignedTo.Value;
        }

        if (dto.IsPacked.HasValue)
            item.IsPacked = dto.IsPacked.Value;

        if (dto.IsShared.HasValue)
            item.IsShared = dto.IsShared.Value;

        if (dto.DayNumber.HasValue)
        {
            ValidateDayNumberRange(dto.DayNumber.Value, trip);
            item.DayNumber = dto.DayNumber.Value;
        }

        await _packingItemRepository.UpdateAsync(item);
        return await EnrichDto(item);
    }

    public async Task Delete(int id, int currentUserId)
    {
        var item = await _packingItemRepository.GetByIdAsync(id);
        if (item == null)
            throw new KeyNotFoundException("Packing item not found");

        if (!await IsTripOwner(item.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can delete packing items");

        await _packingItemRepository.DeleteAsync(item);
    }
}
