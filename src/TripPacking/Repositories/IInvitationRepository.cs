using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public interface IInvitationRepository : IRepository<Invitation>
{
    Task<IEnumerable<Invitation>> GetByTripIdAsync(int tripId);
    Task<IEnumerable<Invitation>> GetByInvitedUserIdAsync(int userId);
    Task<IEnumerable<Invitation>> GetByInvitedByIdAsync(int userId);
    Task<Invitation?> GetPendingByTripAndUserAsync(int tripId, int invitedUserId);
    Task<PagedResult<Invitation>> GetPagedAsync(int pageIndex, int pageSize, int? tripId, InvitationStatus? status, string? direction, int currentUserId);
    Task<IEnumerable<Invitation>> GetExpiredPendingAsync();
}
