using System.ComponentModel.DataAnnotations;

namespace TripPacking.DTOs;

public class CreatePackingTemplateDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    [Required]
    public string ItemsJson { get; set; } = string.Empty;

    [Required]
    public int CreatedBy { get; set; }
}

public class UpdatePackingTemplateDto
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? ItemsJson { get; set; }
}

public class PackingTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string ItemsJson { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PackingTemplateQueryDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Keyword { get; set; }
    public string? Category { get; set; }
}
