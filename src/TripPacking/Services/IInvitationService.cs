using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Services;

public interface IInvitationService
{
    Task<InvitationDto> CreateAsync(CreateInvitationDto dto, int currentUserId);
    Task<InvitationDto> GetByIdAsync(int id, int currentUserId);
    Task<PagedResult<InvitationDto>> GetPagedAsync(InvitationQueryDto query, int currentUserId);
    Task<InvitationDto> RespondAsync(int id, RespondInvitationDto dto, int currentUserId);
    Task CancelAsync(int id, int currentUserId);
    Task<int> ExpireInvitationsAsync();
}
