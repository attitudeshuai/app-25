using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;
using TripPacking.Repositories;

namespace TripPacking.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITripRepository _tripRepository;
    private readonly IMapper _mapper;

    public NotificationService(
        INotificationRepository notificationRepository,
        ITripRepository tripRepository,
        IMapper mapper)
    {
        _notificationRepository = notificationRepository;
        _tripRepository = tripRepository;
        _mapper = mapper;
    }

    private async Task<NotificationDto> EnrichDto(Notification notification)
    {
        var dto = _mapper.Map<NotificationDto>(notification);
        dto.Type = notification.Type.ToString();

        if (notification.RelatedTripId.HasValue)
        {
            var trip = await _tripRepository.GetByIdAsync(notification.RelatedTripId.Value);
            if (trip != null)
                dto.RelatedTripTitle = trip.Title;
        }

        return dto;
    }

    public async Task<PagedResult<NotificationDto>> GetPagedAsync(NotificationQueryDto query, int currentUserId)
    {
        NotificationType? type = null;
        if (!string.IsNullOrWhiteSpace(query.Type) &&
            Enum.TryParse<NotificationType>(query.Type, true, out var parsedType))
            type = parsedType;

        var pagedResult = await _notificationRepository.GetPagedAsync(
            query.PageIndex, query.PageSize, currentUserId, query.IsRead, type);

        var dtos = new List<NotificationDto>();
        foreach (var item in pagedResult.Items)
            dtos.Add(await EnrichDto(item));

        return new PagedResult<NotificationDto> { Items = dtos, Total = pagedResult.Total };
    }

    public async Task<NotificationDto> GetByIdAsync(int id, int currentUserId)
    {
        var notification = await _notificationRepository.GetByIdAsync(id);
        if (notification == null)
            throw new KeyNotFoundException("Notification not found");

        if (notification.UserId != currentUserId)
            throw new UnauthorizedAccessException("No access to this notification");

        return await EnrichDto(notification);
    }

    public async Task<int> GetUnreadCountAsync(int currentUserId)
    {
        return await _notificationRepository.GetUnreadCountAsync(currentUserId);
    }

    public async Task MarkAsReadAsync(int[] notificationIds, int currentUserId)
    {
        if (notificationIds == null || notificationIds.Length == 0)
            return;

        await _notificationRepository.MarkAsReadAsync(notificationIds, currentUserId);
    }

    public async Task MarkAllAsReadAsync(int currentUserId)
    {
        await _notificationRepository.MarkAllAsReadAsync(currentUserId);
    }

    public async Task DeleteAsync(int id, int currentUserId)
    {
        var notification = await _notificationRepository.GetByIdAsync(id);
        if (notification == null)
            throw new KeyNotFoundException("Notification not found");

        if (notification.UserId != currentUserId)
            throw new UnauthorizedAccessException("No access to this notification");

        await _notificationRepository.DeleteAsync(notification);
    }

    private async Task CreateNotificationAsync(
        int userId,
        NotificationType type,
        string title,
        string? content,
        int? tripId,
        int? invitationId)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Content = content,
            RelatedTripId = tripId,
            RelatedInvitationId = invitationId,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification);
    }

    public async Task SendInvitationReceivedAsync(int userId, Trip trip, User inviter, int invitationId, string? message)
    {
        var title = $"{inviter.Username} 邀请你加入旅行「{trip.Title}」";
        var content = string.IsNullOrWhiteSpace(message)
            ? $"你被邀请以成员身份加入旅行「{trip.Title}」，请及时处理。"
            : $"你被邀请加入旅行「{trip.Title}」：{message}";

        await CreateNotificationAsync(userId, NotificationType.InvitationReceived, title, content, trip.Id, invitationId);
    }

    public async Task SendInvitationAcceptedAsync(int userId, Trip trip, User invitedUser, int invitationId)
    {
        var title = $"{invitedUser.Username} 接受了「{trip.Title}」的邀请";
        var content = $"{invitedUser.Username} 已接受加入旅行「{trip.Title}」的邀请。";

        await CreateNotificationAsync(userId, NotificationType.InvitationAccepted, title, content, trip.Id, invitationId);
    }

    public async Task SendInvitationRejectedAsync(int userId, Trip trip, User invitedUser, int invitationId)
    {
        var title = $"{invitedUser.Username} 拒绝了「{trip.Title}」的邀请";
        var content = $"{invitedUser.Username} 已拒绝加入旅行「{trip.Title}」的邀请。";

        await CreateNotificationAsync(userId, NotificationType.InvitationRejected, title, content, trip.Id, invitationId);
    }

    public async Task SendInvitationExpiredAsync(int userId, Trip trip, User invitedUser, int invitationId)
    {
        var title = $"「{trip.Title}」邀请 {invitedUser.Username} 已过期";
        var content = $"发送给 {invitedUser.Username} 的旅行「{trip.Title}」邀请已过期未处理。";

        await CreateNotificationAsync(userId, NotificationType.InvitationExpired, title, content, trip.Id, invitationId);
    }

    public async Task SendInvitationCancelledAsync(int userId, Trip trip, int invitationId)
    {
        var title = $"「{trip.Title}」的邀请已被取消";
        var content = $"加入旅行「{trip.Title}」的邀请已被取消。";

        await CreateNotificationAsync(userId, NotificationType.InvitationCancelled, title, content, trip.Id, invitationId);
    }

    public async Task SendMemberJoinedAsync(int userId, Trip trip, User newMember, int tripId)
    {
        var title = $"{newMember.Username} 加入了旅行「{trip.Title}」";
        var content = $"{newMember.Username} 已正式加入旅行「{trip.Title}」。";

        await CreateNotificationAsync(userId, NotificationType.MemberJoined, title, content, tripId, null);
    }

    public async Task SendMemberRemovedAsync(int userId, Trip trip, User removedMember, int tripId)
    {
        var title = $"{removedMember.Username} 离开了旅行「{trip.Title}」";
        var content = $"{removedMember.Username} 已从旅行「{trip.Title}」中被移除。";

        await CreateNotificationAsync(userId, NotificationType.MemberRemoved, title, content, tripId, null);
    }
}
