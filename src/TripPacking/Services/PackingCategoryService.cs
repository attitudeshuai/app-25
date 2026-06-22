using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class PackingCategoryService : IPackingCategoryService
{
    private readonly IPackingCategoryRepository _packingCategoryRepository;
    private readonly ITripRepository _tripRepository;
    private readonly ITripMemberRepository _tripMemberRepository;
    private readonly IMapper _mapper;

    public PackingCategoryService(IPackingCategoryRepository packingCategoryRepository, ITripRepository tripRepository, ITripMemberRepository tripMemberRepository, IMapper mapper)
    {
        _packingCategoryRepository = packingCategoryRepository;
        _tripRepository = tripRepository;
        _tripMemberRepository = tripMemberRepository;
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

    public async Task<PagedResult<PackingCategoryDto>> GetPaged(PackingCategoryQueryDto query, int currentUserId)
    {
        if (query.TripId.HasValue && !await HasTripAccess(query.TripId.Value, currentUserId))
            throw new UnauthorizedAccessException("No access to this trip");

        var pagedResult = await _packingCategoryRepository.GetPagedAsync(query.PageIndex, query.PageSize, query.Keyword, query.TripId);

        var items = pagedResult.Items;
        var total = pagedResult.Total;

        if (!query.TripId.HasValue)
        {
            var userMemberships = await _tripMemberRepository.GetByUserIdAsync(currentUserId);
            var ownedTrips = await _tripRepository.GetByOwnerIdAsync(currentUserId);
            var accessibleTripIds = userMemberships.Select(m => m.TripId).Concat(ownedTrips.Select(t => t.Id)).ToHashSet();
            items = items.Where(c => accessibleTripIds.Contains(c.TripId)).ToList();
            total = items.Count();
        }

        return new PagedResult<PackingCategoryDto> { Items = _mapper.Map<IEnumerable<PackingCategoryDto>>(items), Total = total };
    }

    public async Task<PackingCategoryDto> GetById(int id, int currentUserId)
    {
        var category = await _packingCategoryRepository.GetByIdAsync(id);
        if (category == null)
            throw new KeyNotFoundException("Packing category not found");

        if (!await HasTripAccess(category.TripId, currentUserId))
            throw new UnauthorizedAccessException("No access to this trip");

        return _mapper.Map<PackingCategoryDto>(category);
    }

    public async Task<PackingCategoryDto> Create(CreatePackingCategoryDto dto, int currentUserId)
    {
        if (!await IsTripOwner(dto.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can create packing categories");

        var category = new PackingCategory
        {
            TripId = dto.TripId,
            Name = dto.Name,
            SortOrder = dto.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        await _packingCategoryRepository.AddAsync(category);
        return _mapper.Map<PackingCategoryDto>(category);
    }

    public async Task<PackingCategoryDto> Update(int id, UpdatePackingCategoryDto dto, int currentUserId)
    {
        var category = await _packingCategoryRepository.GetByIdAsync(id);
        if (category == null)
            throw new KeyNotFoundException("Packing category not found");

        if (!await IsTripOwner(category.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can update packing categories");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            category.Name = dto.Name;

        if (dto.SortOrder.HasValue)
            category.SortOrder = dto.SortOrder.Value;

        await _packingCategoryRepository.UpdateAsync(category);
        return _mapper.Map<PackingCategoryDto>(category);
    }

    public async Task Delete(int id, int currentUserId)
    {
        var category = await _packingCategoryRepository.GetByIdAsync(id);
        if (category == null)
            throw new KeyNotFoundException("Packing category not found");

        if (!await IsTripOwner(category.TripId, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can delete packing categories");

        if (PackingDefaultsInitializerService.IsDefaultCategory(category.Name))
        {
            var trip = await _tripRepository.GetByIdAsync(category.TripId);
            if (trip != null)
            {
                trip.AddDeletedDefaultCategory(category.Name);
                await _tripRepository.UpdateAsync(trip);
            }
        }

        await _packingCategoryRepository.DeleteAsync(category);
    }
}
