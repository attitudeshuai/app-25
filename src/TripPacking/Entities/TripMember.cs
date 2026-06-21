namespace TripPacking.Entities;

public class TripMember
{
    public int Id { get; set; }

    public int TripId { get; set; }

    public int UserId { get; set; }

    public MemberRole Role { get; set; }

    public DateTime JoinedAt { get; set; }

    public Trip Trip { get; set; } = null!;

    public User User { get; set; } = null!;
}
