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

public enum ConflictResolutionStrategy
{
    Skip,
    Rename,
    Overwrite
}

public class TemplateItemDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? Description { get; set; }
    public bool IsShared { get; set; }
    public string? AssignedToRole { get; set; }
}

public class TemplateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public List<TemplateItemDto> Items { get; set; } = new();
    public int SortOrder { get; set; }
}

public class ApplyTemplateRequestDto
{
    [Required]
    public int TemplateId { get; set; }

    [Required]
    public int TripId { get; set; }

    public ConflictResolutionStrategy ConflictStrategy { get; set; } = ConflictResolutionStrategy.Rename;

    public int? DefaultAssignedTo { get; set; }
}

public class AppliedCategoryResultDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public bool IsNew { get; set; }
    public List<AppliedItemResultDto> Items { get; set; } = new();
}

public class AppliedItemResultDto
{
    public string ItemName { get; set; } = string.Empty;
    public int? ItemId { get; set; }
    public bool IsNew { get; set; }
    public bool IsSkipped { get; set; }
    public string? Message { get; set; }
}

public class ApplyTemplateResultDto
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public int TripId { get; set; }
    public int CategoriesCreated { get; set; }
    public int CategoriesSkipped { get; set; }
    public int ItemsCreated { get; set; }
    public int ItemsSkipped { get; set; }
    public int ItemsFailed { get; set; }
    public bool PartialSuccess { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<AppliedCategoryResultDto> Details { get; set; } = new();
}

public class ParsedTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public List<TemplateCategoryDto> Categories { get; set; } = new();
    public int TotalItems { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
