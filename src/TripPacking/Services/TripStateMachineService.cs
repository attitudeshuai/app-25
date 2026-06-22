using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class TripStateMachineService : ITripStateMachineService
{
    private readonly ITripRepository _tripRepository;
    private readonly IPackingItemRepository _packingItemRepository;
    private readonly ITripMemberRepository _tripMemberRepository;
    private readonly ITripStatusHistoryRepository _statusHistoryRepository;
    private readonly IMapper _mapper;

    private static readonly Dictionary<TripStatus, List<TripStatus>> ValidTransitions = new()
    {
        { TripStatus.Planning, new List<TripStatus> { TripStatus.Ongoing } },
        { TripStatus.Ongoing, new List<TripStatus> { TripStatus.Completed, TripStatus.Planning } },
        { TripStatus.Completed, new List<TripStatus> { TripStatus.Ongoing } }
    };

    public TripStateMachineService(
        ITripRepository tripRepository,
        IPackingItemRepository packingItemRepository,
        ITripMemberRepository tripMemberRepository,
        ITripStatusHistoryRepository statusHistoryRepository,
        IMapper mapper)
    {
        _tripRepository = tripRepository;
        _packingItemRepository = packingItemRepository;
        _tripMemberRepository = tripMemberRepository;
        _statusHistoryRepository = statusHistoryRepository;
        _mapper = mapper;
    }

    public bool CanTransition(TripStatus currentStatus, TripStatus targetStatus)
    {
        if (currentStatus == targetStatus)
            return false;

        return ValidTransitions.TryGetValue(currentStatus, out var validTargets)
               && validTargets.Contains(targetStatus);
    }

    public async Task<TripStatusTransitionResult> TransitionStatusAsync(
        int tripId,
        TripStatus targetStatus,
        int currentUserId,
        string? reason)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);
        if (trip == null)
            return new TripStatusTransitionResult { Success = false, Message = "Trip not found" };

        if (!await IsTripOwner(tripId, currentUserId))
            return new TripStatusTransitionResult { Success = false, Message = "Only trip owner can update trip status" };

        var currentStatus = trip.Status;

        if (currentStatus == targetStatus)
            return new TripStatusTransitionResult
            {
                Success = true,
                Message = "Trip is already in target status",
                Trip = _mapper.Map<TripDto>(trip)
            };

        if (!CanTransition(currentStatus, targetStatus))
            return new TripStatusTransitionResult
            {
                Success = false,
                Message = $"Invalid status transition from {currentStatus} to {targetStatus}"
            };

        var isReverseTransition = IsReverseTransition(currentStatus, targetStatus);
        if (isReverseTransition && string.IsNullOrWhiteSpace(reason))
            return new TripStatusTransitionResult
            {
                Success = false,
                Message = "Reverse status transition requires a reason"
            };

        if (targetStatus == TripStatus.Completed)
        {
            var unpackedItems = await GetUnpackedItemsAsync(tripId);
            if (unpackedItems.Any())
                return new TripStatusTransitionResult
                {
                    Success = false,
                    Message = "Cannot complete trip: some items are not packed",
                    UnpackedItems = _mapper.Map<List<PackingItemDto>>(unpackedItems)
                };
        }

        var history = new TripStatusHistory
        {
            TripId = tripId,
            FromStatus = currentStatus,
            ToStatus = targetStatus,
            ChangedBy = currentUserId,
            ChangedAt = DateTime.UtcNow,
            Reason = isReverseTransition ? reason : null
        };

        trip.Status = targetStatus;

        await _statusHistoryRepository.AddAsync(history);
        await _tripRepository.UpdateAsync(trip);

        return new TripStatusTransitionResult
        {
            Success = true,
            Message = "Status updated successfully",
            Trip = _mapper.Map<TripDto>(trip)
        };
    }

    public async Task<IEnumerable<TripStatusHistoryDto>> GetStatusHistoryAsync(int tripId, int currentUserId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);
        if (trip == null)
            throw new KeyNotFoundException("Trip not found");

        if (!await HasTripAccess(tripId, currentUserId))
            throw new UnauthorizedAccessException("No access to this trip");

        var histories = await _statusHistoryRepository.GetByTripIdAsync(tripId);
        return _mapper.Map<IEnumerable<TripStatusHistoryDto>>(histories);
    }

    private async Task<bool> IsTripOwner(int tripId, int userId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);
        return trip != null && trip.OwnerId == userId;
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

    private async Task<List<PackingItem>> GetUnpackedItemsAsync(int tripId)
    {
        var items = await _packingItemRepository.GetByTripIdAsync(tripId);
        return items.Where(i => !i.IsPacked).ToList();
    }

    private bool IsReverseTransition(TripStatus from, TripStatus to)
    {
        return (from == TripStatus.Ongoing && to == TripStatus.Planning)
               || (from == TripStatus.Completed && to == TripStatus.Ongoing)
               || (from == TripStatus.Completed && to == TripStatus.Planning);
    }
}
