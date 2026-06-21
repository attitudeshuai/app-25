using System.ComponentModel.DataAnnotations;

namespace TripPacking.DTOs;

public class CreateTripMemberDto
{
    [Required]
    public int TripId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;
}

public class UpdateTripMemberDto
{
    public string? Role { get; set; }
}

public class TripMemberDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
}

public class TripMemberQueryDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Keyword { get; set; }
    public int? TripId { get; set; }
    public string? Role { get; set; }
}
