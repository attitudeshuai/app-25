using TripPacking.DTOs;

namespace TripPacking.Services;

public interface IPackingItemService
{
    Task<PagedResult<PackingItemDto>> GetPaged(PackingItemQueryDto query, int currentUserId);
    Task<PackingItemDto> GetById(int id, int currentUserId);
    Task<PackingItemDto> Create(CreatePackingItemDto dto, int currentUserId);
    Task<PackingItemDto> Update(int id, UpdatePackingItemDto dto, int currentUserId);
    Task Delete(int id, int currentUserId);
}
