using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public interface IPackingItemRepository : IRepository<PackingItem>
{
    Task<IEnumerable<PackingItem>> GetByTripIdAsync(int tripId);
    Task<PagedResult<PackingItem>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, int? tripId, int? categoryId, bool? isPacked, bool? isShared, int? currentUserId = null);
}
