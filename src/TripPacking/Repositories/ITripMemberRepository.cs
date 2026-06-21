using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public interface ITripMemberRepository : IRepository<TripMember>
{
    Task<IEnumerable<TripMember>> GetByTripIdAsync(int tripId);
    Task<IEnumerable<TripMember>> GetByUserIdAsync(int userId);
    Task<TripMember?> GetByTripAndUserIdAsync(int tripId, int userId);
    Task<PagedResult<TripMember>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, int? tripId, string? role, int? currentUserId = null);
}
