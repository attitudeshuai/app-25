using System.ComponentModel.DataAnnotations;

namespace TripPacking.Entities;

public class Trip
{
    public int Id { get; set; }

    public int OwnerId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Destination { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public TripStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public User Owner { get; set; } = null!;

    public ICollection<TripMember> TripMembers { get; set; } = new List<TripMember>();

    public ICollection<PackingCategory> PackingCategories { get; set; } = new List<PackingCategory>();

    public ICollection<PackingItem> PackingItems { get; set; } = new List<PackingItem>();

    public ICollection<TripStatusHistory> StatusHistories { get; set; } = new List<TripStatusHistory>();
}
