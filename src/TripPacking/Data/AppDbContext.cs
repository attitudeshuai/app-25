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
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<TripStatusHistory> TripStatusHistories => Set<TripStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Status).HasDefaultValue(UserStatus.Active);
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.Property(e => e.Status).HasDefaultValue(TripStatus.Planning);
            entity.Property(e => e.DeletedDefaultCategories).HasDefaultValue(string.Empty);
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

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.Property(e => e.Status).HasDefaultValue(InvitationStatus.Pending);
            entity.HasIndex(e => new { e.TripId, e.InvitedUserId, e.Status });
            entity.HasOne(e => e.Trip)
                .WithMany()
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.InvitedBy)
                .WithMany()
                .HasForeignKey(e => e.InvitedById)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InvitedUser)
                .WithMany()
                .HasForeignKey(e => e.InvitedUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TripStatusHistory>(entity =>
        {
            entity.HasIndex(e => e.TripId);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.StatusHistories)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ChangedByUser)
                .WithMany()
                .HasForeignKey(e => e.ChangedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedData.Seed(modelBuilder);
    }
}
