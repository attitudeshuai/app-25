using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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

    public string DeletedDefaultCategories { get; set; } = string.Empty;

    public User Owner { get; set; } = null!;

    public ICollection<TripMember> TripMembers { get; set; } = new List<TripMember>();

    public ICollection<PackingCategory> PackingCategories { get; set; } = new List<PackingCategory>();

    public ICollection<PackingItem> PackingItems { get; set; } = new List<PackingItem>();

    public ICollection<TripStatusHistory> StatusHistories { get; set; } = new List<TripStatusHistory>();

    public List<string> GetDeletedDefaultCategories()
    {
        if (string.IsNullOrWhiteSpace(DeletedDefaultCategories))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(DeletedDefaultCategories) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void AddDeletedDefaultCategory(string categoryName)
    {
        var deleted = GetDeletedDefaultCategories();
        if (!deleted.Contains(categoryName, StringComparer.OrdinalIgnoreCase))
        {
            deleted.Add(categoryName);
            DeletedDefaultCategories = JsonSerializer.Serialize(deleted);
        }
    }
}
