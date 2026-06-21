using System.ComponentModel.DataAnnotations;

namespace TripPacking.Entities;

public class PackingTemplate
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public string ItemsJson { get; set; } = string.Empty;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public User Creator { get; set; } = null!;
}
