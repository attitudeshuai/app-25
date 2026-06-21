using TripPacking.DTOs;

namespace TripPacking.Services;

public interface ITripMemberService
{
    Task<PagedResult<TripMemberDto>> GetPaged(TripMemberQueryDto query, int currentUserId);
    Task<TripMemberDto> GetById(int id, int currentUserId);
    Task<TripMemberDto> Create(CreateTripMemberDto dto, int currentUserId);
    Task<TripMemberDto> Update(int id, UpdateTripMemberDto dto, int currentUserId);
    Task Delete(int id, int currentUserId);
    Task<PagedResult<TripMemberDto>> GetMine(int currentUserId, TripMemberQueryDto query);
}
