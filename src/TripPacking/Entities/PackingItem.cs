using System.ComponentModel.DataAnnotations;

namespace TripPacking.Entities;

public class PackingItem
{
    public int Id { get; set; }

    public int TripId { get; set; }

    public int CategoryId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int? AssignedTo { get; set; }

    public bool IsPacked { get; set; }

    public bool IsShared { get; set; }

    public int? DayNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    public Trip Trip { get; set; } = null!;

    public PackingCategory Category { get; set; } = null!;

    public User? AssignedUser { get; set; }
}
