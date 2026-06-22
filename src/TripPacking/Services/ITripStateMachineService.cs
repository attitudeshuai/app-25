using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Services;

public interface ITripStateMachineService
{
    Task<TripStatusTransitionResult> TransitionStatusAsync(int tripId, TripStatus targetStatus, int currentUserId, string? reason);
    bool CanTransition(TripStatus currentStatus, TripStatus targetStatus);
    Task<IEnumerable<TripStatusHistoryDto>> GetStatusHistoryAsync(int tripId, int currentUserId);
}
