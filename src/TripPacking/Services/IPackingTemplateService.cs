using TripPacking.DTOs;

namespace TripPacking.Services;

public interface IPackingTemplateService
{
    Task<PagedResult<PackingTemplateDto>> GetPaged(PackingTemplateQueryDto query);
    Task<PackingTemplateDto> GetById(int id);
    Task<PackingTemplateDto> Create(CreatePackingTemplateDto dto, int currentUserId);
    Task<PackingTemplateDto> Update(int id, UpdatePackingTemplateDto dto, int currentUserId);
    Task Delete(int id, int currentUserId);
}
