using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class TripService : ITripService
{
    private readonly ITripRepository _tripRepository;
    private readonly ITripMemberRepository _tripMemberRepository;
    private readonly ITripStateMachineService _stateMachineService;
    private readonly IMapper _mapper;

    public TripService(
        ITripRepository tripRepository,
        ITripMemberRepository tripMemberRepository,
        ITripStateMachineService stateMachineService,
        IMapper mapper)
    {
        _tripRepository = tripRepository;
        _tripMemberRepository = tripMemberRepository;
        _stateMachineService = stateMachineService;
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

    public async Task<PagedResult<TripDto>> GetPaged(TripQueryDto query, int? userId)
    {
        TripStatus? status = query.Status.HasValue ? (TripStatus)query.Status.Value : null;
        var pagedResult = await _tripRepository.GetPagedAsync(query.PageIndex, query.PageSize, query.Keyword, status);

        var items = pagedResult.Items;
        var total = pagedResult.Total;

        if (userId.HasValue)
        {
            var userMemberships = await _tripMemberRepository.GetByUserIdAsync(userId.Value);
            var accessibleTripIds = userMemberships.Select(m => m.TripId).ToHashSet();
            var ownedTrips = await _tripRepository.GetByOwnerIdAsync(userId.Value);
            foreach (var t in ownedTrips)
                accessibleTripIds.Add(t.Id);

            items = items.Where(t => accessibleTripIds.Contains(t.Id)).ToList();
            total = items.Count();
        }

        return new PagedResult<TripDto> { Items = _mapper.Map<IEnumerable<TripDto>>(items), Total = total };
    }

    public async Task<TripDto> GetById(int id, int currentUserId)
    {
        var trip = await _tripRepository.GetByIdAsync(id);
        if (trip == null)
            throw new KeyNotFoundException("Trip not found");

        if (!await HasTripAccess(id, currentUserId))
            throw new UnauthorizedAccessException("No access to this trip");

        return _mapper.Map<TripDto>(trip);
    }

    public async Task<TripDto> Create(CreateTripDto dto, int ownerId)
    {
        var trip = new Trip
        {
            OwnerId = ownerId,
            Title = dto.Title,
            Destination = dto.Destination ?? string.Empty,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = TripStatus.Planning,
            CreatedAt = DateTime.UtcNow
        };

        await _tripRepository.AddAsync(trip);

        var ownerMember = new TripMember
        {
            TripId = trip.Id,
            UserId = ownerId,
            Role = MemberRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        await _tripMemberRepository.AddAsync(ownerMember);
        return _mapper.Map<TripDto>(trip);
    }

    public async Task<TripDto> Update(int id, UpdateTripDto dto, int currentUserId)
    {
        var trip = await _tripRepository.GetByIdAsync(id);
        if (trip == null)
            throw new KeyNotFoundException("Trip not found");

        if (!await IsTripOwner(id, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can update this trip");

        if (!string.IsNullOrWhiteSpace(dto.Title))
            trip.Title = dto.Title;

        if (dto.Destination != null)
            trip.Destination = dto.Destination;

        if (dto.StartDate.HasValue)
            trip.StartDate = dto.StartDate.Value;

        if (dto.EndDate.HasValue)
            trip.EndDate = dto.EndDate.Value;

        await _tripRepository.UpdateAsync(trip);
        return _mapper.Map<TripDto>(trip);
    }

    public async Task Delete(int id, int currentUserId)
    {
        var trip = await _tripRepository.GetByIdAsync(id);
        if (trip == null)
            throw new KeyNotFoundException("Trip not found");

        if (!await IsTripOwner(id, currentUserId))
            throw new UnauthorizedAccessException("Only trip owner can delete this trip");

        await _tripRepository.DeleteAsync(trip);
    }

    public async Task<TripStatusTransitionResult> UpdateStatus(int id, UpdateTripStatusDto dto, int currentUserId)
    {
        var targetStatus = (TripStatus)dto.Status;
        return await _stateMachineService.TransitionStatusAsync(id, targetStatus, currentUserId, dto.Reason);
    }

    public async Task<IEnumerable<TripStatusHistoryDto>> GetStatusHistory(int id, int currentUserId)
    {
        return await _stateMachineService.GetStatusHistoryAsync(id, currentUserId);
    }

    public async Task<PagedResult<TripDto>> GetMine(int currentUserId, TripQueryDto query)
    {
        var ownedTrips = await _tripRepository.GetByOwnerIdAsync(currentUserId);
        var memberships = await _tripMemberRepository.GetByUserIdAsync(currentUserId);
        var memberTripIds = memberships.Select(m => m.TripId).ToHashSet();

        var allTrips = ownedTrips.ToList();
        foreach (var tripId in memberTripIds)
        {
            if (!allTrips.Any(t => t.Id == tripId))
            {
                var trip = await _tripRepository.GetByIdAsync(tripId);
                if (trip != null)
                    allTrips.Add(trip);
            }
        }

        TripStatus? status = query.Status.HasValue ? (TripStatus)query.Status.Value : null;

        var filtered = allTrips.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            filtered = filtered.Where(t => t.Title.Contains(query.Keyword!));

        if (status.HasValue)
            filtered = filtered.Where(t => t.Status == status.Value);

        var total = filtered.Count();
        var items = filtered.Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize).ToList();

        return new PagedResult<TripDto> { Items = _mapper.Map<IEnumerable<TripDto>>(items), Total = total };
    }
}
