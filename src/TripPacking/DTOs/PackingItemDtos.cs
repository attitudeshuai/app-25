using System.ComponentModel.DataAnnotations;

namespace TripPacking.DTOs;

public class CreatePackingItemDto
{
    [Required]
    public int TripId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int Quantity { get; set; }

    public int? AssignedTo { get; set; }

    public bool IsPacked { get; set; }

    public bool IsShared { get; set; }

    public int? DayNumber { get; set; }
}

public class UpdatePackingItemDto
{
    public int? CategoryId { get; set; }
    public string? Name { get; set; }
    public int? Quantity { get; set; }
    public int? AssignedTo { get; set; }
    public bool? IsPacked { get; set; }
    public bool? IsShared { get; set; }
    public int? DayNumber { get; set; }
}

public class PackingItemDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? AssignedTo { get; set; }
    public bool IsPacked { get; set; }
    public bool IsShared { get; set; }
    public int? DayNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AssignedUsername { get; set; }
}

public class PackingItemQueryDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Keyword { get; set; }
    public int? TripId { get; set; }
    public int? CategoryId { get; set; }
    public bool? IsPacked { get; set; }
    public bool? IsShared { get; set; }
}
