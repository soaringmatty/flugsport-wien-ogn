using Microsoft.EntityFrameworkCore;
using FlugsportWienOgn.Database.Entities;

namespace FlugsportWienOgn.Database;

public class FlightDbContext(DbContextOptions<FlightDbContext> options) : DbContext(options)
{
    public DbSet<Aircraft> Aircraft { get; set; }
    public DbSet<FlightPathItem> FlightData { get; set; }
    public DbSet<KnownAircraft> KnownAircraft { get; set; }
    public DbSet<CleanupStamp> Cleanup { get; set; }

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

        modelBuilder.Entity<Aircraft>()
            .HasIndex(x => x.FlarmId)
            .IsUnique();

        modelBuilder.Entity<FlightPathItem>()
            .HasIndex(x => x.AircraftId);

        modelBuilder.Entity<KnownAircraft>()
            .HasIndex(x => x.FlarmId)
            .IsUnique();

        // Data seeds
        modelBuilder.Entity<CleanupStamp>()
            .HasData(new CleanupStamp
            {
                Id = 1,
                LastCleanup = DateOnly.FromDateTime(DateTime.Now.AddDays(-1))
            });

        modelBuilder.Entity<KnownAircraft>().HasData(
            // ----- Club Gliders -------------------------------------------------
            new KnownAircraft { Id = 1, FlarmId = "DD98B4", Registration = "D-0544", RegistrationShort = "DR", Model = "Ventus 2b", AircraftType = 1, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },
            new KnownAircraft { Id = 2, FlarmId = "DD9EA3", Registration = "D-2526", RegistrationShort = "DL", Model = "LS4", AircraftType = 1, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },
            new KnownAircraft { Id = 3, FlarmId = "DDAD0C", Registration = "D-3931", RegistrationShort = "DZ", Model = "ASK 21", AircraftType = 1, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },
            new KnownAircraft { Id = 4, FlarmId = "DD98C1", Registration = "D-9104", RegistrationShort = "DV", Model = "LS4", AircraftType = 1, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },
            new KnownAircraft { Id = 5, FlarmId = "3F0625", Registration = "D-9614", RegistrationShort = "D2B", Model = "Discus‑2b", AircraftType = 1, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },
            new KnownAircraft { Id = 6, FlarmId = "DD9537", Registration = "OE-5446", RegistrationShort = "DX", Model = "ASK 21", AircraftType = 1, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },
            new KnownAircraft { Id = 7, FlarmId = "DDAF0B", Registration = "OE-5491", RegistrationShort = "91", Model = "DG 300 Elan", AircraftType = 1, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },
            new KnownAircraft { Id = 8, FlarmId = "4404FD", Registration = "OE-5603", RegistrationShort = "DS", Model = "Ventus 2b", AircraftType = 1, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },
            new KnownAircraft { Id = 9, FlarmId = "DD9F86", Registration = "OE-5711", RegistrationShort = "DI", Model = "DG 500 Orion", AircraftType = 1, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },

            // ----- Club Motorplanes -------------------------------------------------
            new KnownAircraft { Id = 20, FlarmId = "DD9382", Registration = "D-KRES", RegistrationShort = "RES", Model = "Dimona HK 36 TTC", AircraftType = 3, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },
            new KnownAircraft { Id = 21, FlarmId = "440524", Registration = "OE-9466", RegistrationShort = "466", Model = "Dimona HK 36 TTC", AircraftType = 3, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },
            new KnownAircraft { Id = 22, FlarmId = "440523", Registration = "OE-CBB", RegistrationShort = "CBB", Model = "Katana DA 20 A1", AircraftType = 3, OwnershipType = 0, Owner = "ASKÖ Flugsport Wien" },

            // ----- Private Gliders ---------------------------------------------
            new KnownAircraft { Id = 100, FlarmId = "3ECF12", Registration = "D-KTJM", RegistrationShort = "FH", Model = "Valentin Kiwi", AircraftType = 1, OwnershipType = 1, Owner = "Fabian Hoffmann" },
            new KnownAircraft { Id = 101, FlarmId = "D0114B", Registration = "D-6000", RegistrationShort = "MI", Model = "DG‑600", AircraftType = 1, OwnershipType = 1, Owner = "Andreas Stocker" },
            new KnownAircraft { Id = 102, FlarmId = "DDFE83", Registration = "D-2254", RegistrationShort = "HR", Model = "LS1‑f", AircraftType = 1, OwnershipType = 1, Owner = "Julia Götz" },
            new KnownAircraft { Id = 103, FlarmId = "3EFBF6", Registration = "D-7007", RegistrationShort = "SE", Model = "Mini Nimbus", AircraftType = 1, OwnershipType = 1, Owner = "Ernst Schicker" },
            //new KnownAircraft { Id = ??, FlarmId = "??", Registration = "D-3533", RegistrationShort = "SC", Model = "Ventus", AircraftType = 1, OwnershipType = 1, Owner = "Ernst Schicker" },
            new KnownAircraft { Id = 104, FlarmId = "D0019F", Registration = "D-KEVA", RegistrationShort = "O2", Model = "DG‑800", AircraftType = 1, OwnershipType = 1, Owner = "Stephan Haupt" },
            new KnownAircraft { Id = 105, FlarmId = "3EE707", Registration = "D-1648", RegistrationShort = "DE", Model = "ASW‑20", AircraftType = 1, OwnershipType = 1, Owner = "Thomas Dvorak" },
            new KnownAircraft { Id = 106, FlarmId = "F64550", Registration = "D-1890", RegistrationShort = "KA8", Model = "Ka‑8", AircraftType = 1, OwnershipType = 1, Owner = "Christoph Urach" },
            new KnownAircraft { Id = 107, FlarmId = "DDB289", Registration = "D-KXPP", RegistrationShort = "PP", Model = "ASH‑26 E", AircraftType = 1, OwnershipType = 1, Owner = "Irmgard Paul" },
            new KnownAircraft { Id = 108, FlarmId = "3EEC8B", Registration = "D-3060", RegistrationShort = "CZ", Model = "HPH 304CZ‑17", AircraftType = 1, OwnershipType = 1, Owner = "Sören Rossow" }, // TODO (DF0F03)
            new KnownAircraft { Id = 109, FlarmId = "3EFBA7", Registration = "D-6928", RegistrationShort = "SI", Model = "ASW 28", AircraftType = 1, OwnershipType = 1, Owner = "Mario Neumann" }
        );
    }
}
