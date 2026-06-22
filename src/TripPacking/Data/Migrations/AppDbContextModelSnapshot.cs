using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TripPacking.Entities;

namespace TripPacking.Data.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 64);

        modelBuilder.Entity("TripPacking.Entities.User", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            b.Property<string>("Username")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            b.Property<string>("Email")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("varchar(100)");

            b.Property<string>("PasswordHash")
                .IsRequired()
                .HasColumnType("longtext");

            b.Property<string>("Avatar")
                .HasMaxLength(500)
                .HasColumnType("varchar(500)");

            b.Property<UserStatus>("Status")
                .HasColumnType("int")
                .HasDefaultValue(UserStatus.Active);

            b.Property<PasswordHashVersion>("PasswordHashVersion")
                .HasColumnType("int")
                .HasDefaultValue(PasswordHashVersion.Sha256);

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime(6)");

            b.Property<DateTime?>("UpdatedAt")
                .HasColumnType("datetime(6)");

            b.HasKey("Id");

            b.HasIndex("Username")
                .IsUnique();

            b.HasIndex("Email")
                .IsUnique();

            b.ToTable("Users");
        });
    }
}
