namespace TripPacking.DTOs;

public class StatsOverviewDto
{
    public int TotalUsers { get; set; }
    public int TotalTrips { get; set; }
    public int TotalItems { get; set; }
    public int PackedItems { get; set; }
    public int UnpackedItems { get; set; }
    public double PackingProgress { get; set; }
}

public class StatsTrendDto
{
    public string Date { get; set; } = string.Empty;
    public int Trips { get; set; }
    public int Items { get; set; }
}
