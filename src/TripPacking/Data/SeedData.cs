using Microsoft.EntityFrameworkCore;
using TripPacking.Entities;

namespace TripPacking.Data;

public static class SeedData
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "user1", Email = "user1@test.com", PasswordHash = "JAH7Gx5w6b0Gf7zU6zU5zU4zU3zU2zU1zU0zU/zU8zU7zU6zU5zU4zU3zU2zU1zU0=", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new User { Id = 2, Username = "user2", Email = "user2@test.com", PasswordHash = "JAH7Gx5w6b0Gf7zU6zU5zU4zU3zU2zU1zU0zU/zU8zU7zU6zU5zU4zU3zU2zU1zU0=", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new User { Id = 3, Username = "user3", Email = "user3@test.com", PasswordHash = "JAH7Gx5w6b0Gf7zU6zU5zU4zU3zU2zU1zU0zU/zU8zU7zU6zU5zU4zU3zU2zU1zU0=", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<Trip>().HasData(
            new Trip { Id = 1, OwnerId = 1, Title = "Beach Vacation", Destination = "Maldives", StartDate = new DateTime(2024, 1, 15), EndDate = new DateTime(2024, 1, 25), Status = TripStatus.Planning, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Trip { Id = 2, OwnerId = 2, Title = "Mountain Hiking", Destination = "Alps", StartDate = new DateTime(2024, 2, 10), EndDate = new DateTime(2024, 2, 20), Status = TripStatus.Ongoing, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Trip { Id = 3, OwnerId = 1, Title = "City Tour", Destination = "Paris", StartDate = new DateTime(2024, 3, 5), EndDate = new DateTime(2024, 3, 12), Status = TripStatus.Completed, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<TripMember>().HasData(
            new TripMember { Id = 1, TripId = 1, UserId = 1, Role = MemberRole.Owner, JoinedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new TripMember { Id = 2, TripId = 1, UserId = 2, Role = MemberRole.Member, JoinedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
            new TripMember { Id = 3, TripId = 2, UserId = 2, Role = MemberRole.Owner, JoinedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new TripMember { Id = 4, TripId = 2, UserId = 3, Role = MemberRole.Member, JoinedAt = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc) },
            new TripMember { Id = 5, TripId = 3, UserId = 1, Role = MemberRole.Owner, JoinedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<PackingCategory>().HasData(
            new PackingCategory { Id = 1, TripId = 1, Name = "Clothing", SortOrder = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingCategory { Id = 2, TripId = 1, Name = "Electronics", SortOrder = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingCategory { Id = 3, TripId = 2, Name = "Toiletries", SortOrder = 3, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingCategory { Id = 4, TripId = 3, Name = "Gear", SortOrder = 4, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<PackingItem>().HasData(
            new PackingItem { Id = 1, TripId = 1, CategoryId = 1, Name = "Swimsuit", Quantity = 2, IsPacked = false, IsShared = false, DayNumber = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingItem { Id = 2, TripId = 1, CategoryId = 1, Name = "Sun Hat", Quantity = 1, IsPacked = true, IsShared = false, AssignedTo = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingItem { Id = 3, TripId = 1, CategoryId = 2, Name = "Phone Charger", Quantity = 1, IsPacked = false, IsShared = true, AssignedTo = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingItem { Id = 4, TripId = 1, CategoryId = 2, Name = "Camera", Quantity = 1, IsPacked = true, IsShared = true, DayNumber = 3, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingItem { Id = 5, TripId = 2, CategoryId = 3, Name = "Sunscreen", Quantity = 1, IsPacked = false, IsShared = true, AssignedTo = 3, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingItem { Id = 6, TripId = 2, CategoryId = 3, Name = "Toothbrush", Quantity = 1, IsPacked = true, IsShared = false, DayNumber = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingItem { Id = 7, TripId = 3, CategoryId = 4, Name = "Walking Shoes", Quantity = 1, IsPacked = false, IsShared = false, AssignedTo = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingItem { Id = 8, TripId = 3, CategoryId = 4, Name = "Backpack", Quantity = 1, IsPacked = true, IsShared = true, DayNumber = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<PackingTemplate>().HasData(
            new PackingTemplate { Id = 1, Name = "Beach Essentials", Category = "Vacation", ItemsJson = "[\"Swimsuit\",\"Sunscreen\",\"Towel\",\"Sunglasses\"]", CreatedBy = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingTemplate { Id = 2, Name = "Hiking Gear", Category = "Outdoor", ItemsJson = "[\"Boots\",\"Rain Jacket\",\"First Aid Kit\",\"Water Bottle\"]", CreatedBy = 2, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PackingTemplate { Id = 3, Name = "Business Trip", Category = "Work", ItemsJson = "[\"Laptop\",\"Suit\",\"Documents\",\"Charger\"]", CreatedBy = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
