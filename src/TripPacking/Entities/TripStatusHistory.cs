using System.ComponentModel.DataAnnotations;

namespace TripPacking.Entities;

public class TripStatusHistory
{
    public int Id { get; set; }

    public int TripId { get; set; }

    public TripStatus FromStatus { get; set; }

    public TripStatus ToStatus { get; set; }

    public int ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public Trip Trip { get; set; } = null!;

    public User ChangedByUser { get; set; } = null!;
}
