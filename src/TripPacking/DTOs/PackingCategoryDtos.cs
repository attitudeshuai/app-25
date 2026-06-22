using System.ComponentModel.DataAnnotations;

namespace TripPacking.DTOs;

public class CreatePackingCategoryDto
{
    [Required]
    public int TripId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0, 10000, ErrorMessage = "排序序号必须在0到10000之间")]
    public int SortOrder { get; set; }
}

public class UpdatePackingCategoryDto
{
    public string? Name { get; set; }

    [Range(0, 10000, ErrorMessage = "排序序号必须在0到10000之间")]
    public int? SortOrder { get; set; }
}

public class UpdateCategorySortOrderDto
{
    [Required]
    public int TripId { get; set; }

    [Required]
    public List<CategorySortOrderItemDto> Items { get; set; } = new();
}

public class CategorySortOrderItemDto
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [Range(0, 10000, ErrorMessage = "排序序号必须在0到10000之间")]
    public int SortOrder { get; set; }
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
