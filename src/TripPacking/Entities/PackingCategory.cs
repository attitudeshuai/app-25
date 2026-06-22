using System.ComponentModel.DataAnnotations;

namespace TripPacking.Entities;

public class PackingCategory
{
    public int Id { get; set; }

    public int TripId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 10000, ErrorMessage = "排序序号必须在0到10000之间")]
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public Trip Trip { get; set; } = null!;

    public ICollection<PackingItem> PackingItems { get; set; } = new List<PackingItem>();
}
