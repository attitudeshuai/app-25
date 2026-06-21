using TripPacking.DTOs;

namespace TripPacking.Services;

public interface IPackingCategoryService
{
    Task<PagedResult<PackingCategoryDto>> GetPaged(PackingCategoryQueryDto query, int currentUserId);
    Task<PackingCategoryDto> GetById(int id, int currentUserId);
    Task<PackingCategoryDto> Create(CreatePackingCategoryDto dto, int currentUserId);
    Task<PackingCategoryDto> Update(int id, UpdatePackingCategoryDto dto, int currentUserId);
    Task Delete(int id, int currentUserId);
}
