using System.ComponentModel.DataAnnotations;

namespace TripPacking.Entities;

public class Invitation
{
    public int Id { get; set; }

    public int TripId { get; set; }

    public int InvitedById { get; set; }

    public int InvitedUserId { get; set; }

    public MemberRole Role { get; set; }

    public InvitationStatus Status { get; set; }

    public DateTime ExpiresAt { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public Trip Trip { get; set; } = null!;

    public User InvitedBy { get; set; } = null!;

    public User InvitedUser { get; set; } = null!;
}
