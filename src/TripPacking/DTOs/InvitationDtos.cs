using System.ComponentModel.DataAnnotations;

namespace TripPacking.DTOs;

public class CreateInvitationDto
{
    [Required]
    public int TripId { get; set; }

    [Required]
    public int InvitedUserId { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Message { get; set; }

    public int? ExpiryHours { get; set; }
}

public class InvitationDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string? TripTitle { get; set; }
    public string? TripDestination { get; set; }
    public int InvitedById { get; set; }
    public string? InvitedByUsername { get; set; }
    public string? InvitedByAvatar { get; set; }
    public int InvitedUserId { get; set; }
    public string? InvitedUserUsername { get; set; }
    public string? InvitedUserEmail { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class InvitationQueryDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? TripId { get; set; }
    public string? Status { get; set; }
    public string? Direction { get; set; }
}

public class RespondInvitationDto
{
    [Required]
    public bool Accept { get; set; }
}
