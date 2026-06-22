using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TripPacking.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql("Server=localhost;Database=trippacking;User=root;Password=root;",
            ServerVersion.AutoDetect("Server=localhost;Database=trippacking;User=root;Password=root;"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
