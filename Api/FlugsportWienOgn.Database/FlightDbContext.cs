using Microsoft.EntityFrameworkCore;
using FlugsportWienOgn.Database.Entities;

namespace FlugsportWienOgn.Database;

public class FlightDbContext(DbContextOptions<FlightDbContext> options) : DbContext(options)
{
    public DbSet<Plane> Planes { get; set; }
    public DbSet<FlightPathItem> FlightData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plane>()
            .HasMany(p => p.FlightPath)
            .WithOne(fd => fd.Plane)
            .HasForeignKey(fd => fd.PlaneId);
    }
}
