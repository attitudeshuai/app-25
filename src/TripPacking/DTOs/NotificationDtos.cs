using System.ComponentModel.DataAnnotations;

namespace TripPacking.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int? RelatedTripId { get; set; }
    public string? RelatedTripTitle { get; set; }
    public int? RelatedInvitationId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class NotificationQueryDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool? IsRead { get; set; }
    public string? Type { get; set; }
}

public class MarkNotificationsReadDto
{
    public int[]? NotificationIds { get; set; }
}
