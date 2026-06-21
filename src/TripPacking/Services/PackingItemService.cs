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
    private readonly IMapper _mapper;

    public PackingItemService(IPackingItemRepository packingItemRepository, ITripRepository tripRepository, ITripMemberRepository tripMemberRepository, IUserRepository userRepository, IMapper mapper)
    {
        _packingItemRepository = packingItemRepository;
        _tripRepository = tripRepository;
        _tripMemberRepository = tripMemberRepository;
        _userRepository = userRepository;
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

    private async Task<bool> HasTripMemberAccess(int tripId, int userId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);
        if (trip == null)
            return false;

        if (trip.OwnerId == userId)
            return true;

        var members = await _tripMemberRepository.GetByTripIdAsync(tripId);
        return members.Any(m => m.UserId == userId);
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
        if (!await HasTripMemberAccess(dto.TripId, currentUserId))
            throw new UnauthorizedAccessException("No access to create items for this trip");

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

        if (dto.CategoryId.HasValue)
            item.CategoryId = dto.CategoryId.Value;

        if (!string.IsNullOrWhiteSpace(dto.Name))
            item.Name = dto.Name;

        if (dto.Quantity.HasValue)
            item.Quantity = dto.Quantity.Value;

        if (dto.AssignedTo.HasValue)
            item.AssignedTo = dto.AssignedTo.Value;

        if (dto.IsPacked.HasValue)
            item.IsPacked = dto.IsPacked.Value;

        if (dto.IsShared.HasValue)
            item.IsShared = dto.IsShared.Value;

        if (dto.DayNumber.HasValue)
            item.DayNumber = dto.DayNumber.Value;

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
