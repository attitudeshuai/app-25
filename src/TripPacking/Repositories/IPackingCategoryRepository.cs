using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public interface IPackingCategoryRepository : IRepository<PackingCategory>
{
    Task<IEnumerable<PackingCategory>> GetByTripIdAsync(int tripId);
    Task<PagedResult<PackingCategory>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, int? tripId, int? currentUserId = null);
    Task<bool> IsSortOrderDuplicateAsync(int tripId, int sortOrder, int? excludeCategoryId = null);
    Task<IEnumerable<PackingCategory>> GetByIdsAsync(IEnumerable<int> ids);
    Task UpdateRangeAsync(IEnumerable<PackingCategory> categories);
}
