using Microsoft.EntityFrameworkCore;
using FlugsportWienOgn.Database.Entities;

namespace FlugsportWienOgn.Database;

public class FlightDbContext(DbContextOptions<FlightDbContext> options) : DbContext(options)
{
    public DbSet<Aircraft> Aircraft { get; set; }
    public DbSet<FlightPathItem> FlightData { get; set; }

    public void InitializeDatabase()
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Aircraft>()
            .HasMany(p => p.FlightPath)
            .WithOne(fd => fd.Aircraft)
            .HasForeignKey(fd => fd.AircraftId);
    }
}
