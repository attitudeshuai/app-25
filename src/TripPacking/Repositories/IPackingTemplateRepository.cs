using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Repositories;

public interface IPackingTemplateRepository : IRepository<PackingTemplate>
{
    Task<PagedResult<PackingTemplate>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, string? category);
}
