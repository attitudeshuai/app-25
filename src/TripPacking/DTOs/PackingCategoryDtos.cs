using System.ComponentModel.DataAnnotations;

namespace TripPacking.DTOs;

public class CreatePackingCategoryDto
{
    [Required]
    public int TripId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int SortOrder { get; set; }
}

public class UpdatePackingCategoryDto
{
    public string? Name { get; set; }
    public int? SortOrder { get; set; }
}

public class PackingCategoryDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PackingCategoryQueryDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Keyword { get; set; }
    public int? TripId { get; set; }
}
