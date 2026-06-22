using TripPacking.DTOs;

namespace TripPacking.Services;

public interface ITripService
{
    Task<PagedResult<TripDto>> GetPaged(TripQueryDto query, int? userId);
    Task<TripDto> GetById(int id, int currentUserId);
    Task<TripDto> Create(CreateTripDto dto, int ownerId);
    Task<TripDto> Update(int id, UpdateTripDto dto, int currentUserId);
    Task Delete(int id, int currentUserId);
    Task<TripStatusTransitionResult> UpdateStatus(int id, UpdateTripStatusDto dto, int currentUserId);
    Task<PagedResult<TripDto>> GetMine(int currentUserId, TripQueryDto query);
    Task<IEnumerable<TripStatusHistoryDto>> GetStatusHistory(int id, int currentUserId);
}
