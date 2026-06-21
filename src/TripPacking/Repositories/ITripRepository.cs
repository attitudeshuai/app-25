using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public interface ITripRepository : IRepository<Trip>
{
    Task<IEnumerable<Trip>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Trip>> GetByOwnerIdAsync(int ownerId);
    Task<PagedResult<Trip>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, TripStatus? status, int? userId = null);
}
