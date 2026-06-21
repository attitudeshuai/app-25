using System.ComponentModel.DataAnnotations;

namespace TripPacking.Entities;

public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public NotificationType Type { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Content { get; set; }

    public int? RelatedTripId { get; set; }

    public int? RelatedInvitationId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public User User { get; set; } = null!;
}
