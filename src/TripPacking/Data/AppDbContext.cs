using Microsoft.EntityFrameworkCore;
using TripPacking.Entities;

namespace TripPacking.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripMember> TripMembers => Set<TripMember>();
    public DbSet<PackingCategory> PackingCategories => Set<PackingCategory>();
    public DbSet<PackingItem> PackingItems => Set<PackingItem>();
    public DbSet<PackingTemplate> PackingTemplates => Set<PackingTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.Property(e => e.Status).HasDefaultValue(TripStatus.Planning);
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TripMember>(entity =>
        {
            entity.HasIndex(e => new { e.TripId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.TripMembers)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PackingCategory>(entity =>
        {
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.PackingCategories)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PackingItem>(entity =>
        {
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.PackingItems)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Category)
                .WithMany(c => c.PackingItems)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AssignedUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PackingTemplate>(entity =>
        {
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedData.Seed(modelBuilder);
    }
}
