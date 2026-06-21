using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetPagedAsync(NotificationQueryDto query, int currentUserId);
    Task<NotificationDto> GetByIdAsync(int id, int currentUserId);
    Task<int> GetUnreadCountAsync(int currentUserId);
    Task MarkAsReadAsync(int[] notificationIds, int currentUserId);
    Task MarkAllAsReadAsync(int currentUserId);
    Task DeleteAsync(int id, int currentUserId);

    Task SendInvitationReceivedAsync(int userId, Trip trip, User inviter, int invitationId, string? message);
    Task SendInvitationAcceptedAsync(int userId, Trip trip, User invitedUser, int invitationId);
    Task SendInvitationRejectedAsync(int userId, Trip trip, User invitedUser, int invitationId);
    Task SendInvitationExpiredAsync(int userId, Trip trip, User invitedUser, int invitationId);
    Task SendInvitationCancelledAsync(int userId, Trip trip, int invitationId);
    Task SendMemberJoinedAsync(int userId, Trip trip, User newMember, int tripId);
    Task SendMemberRemovedAsync(int userId, Trip trip, User removedMember, int tripId);
}
