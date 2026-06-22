using System.ComponentModel.DataAnnotations;

namespace TripPacking.DTOs;

public class CreateTripDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Destination { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class UpdateTripDto
{
    public string? Title { get; set; }
    public string? Destination { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class TripDto
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateTripStatusDto
{
    [Required]
    public int Status { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class TripStatusTransitionResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TripDto? Trip { get; set; }
    public List<PackingItemDto>? UnpackedItems { get; set; }
}

public class TripStatusHistoryDto
{
    public int Id { get; set; }
    public int FromStatus { get; set; }
    public int ToStatus { get; set; }
    public int ChangedBy { get; set; }
    public string? ChangedByUserName { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}

public class TripQueryDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Keyword { get; set; }
    public int? Status { get; set; }
}
