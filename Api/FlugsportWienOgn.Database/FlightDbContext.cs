using Microsoft.EntityFrameworkCore;
using FlugsportWienOgn.Database.Entities;
using Microsoft.Extensions.Logging;
using FlugsportWienOgn.Database.Seeds;
using System.Text.Json;

namespace FlugsportWienOgn.Database;

public class FlightDbContext : DbContext
{
    public DbSet<Aircraft> Aircraft { get; set; }
    public DbSet<FlightPathItem> FlightData { get; set; }
    public DbSet<KnownAircraft> KnownAircraft { get; set; }
    public DbSet<FlightbookEntry> FlightbookEntry { get; set; }
    public DbSet<GliderModel> GliderModel { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FlightDbContext> _logger;

    public FlightDbContext(DbContextOptions<FlightDbContext> options, ILogger<FlightDbContext> logger, IServiceProvider serviceProvider)
        : base(options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void InitializeDatabase()
    {
        const int maxRetries = 10;
        var retries = 0;
        while (true)
        {
            try
            {
                Database.Migrate();
                _logger.LogInformation("Database has been checked (created or migrated if necessary)");
                InitializeGliderModelsFromJson();
                _logger.LogInformation("Database glider models have been initialized");
                return;
            }
            catch (Exception ex)
            {
                if (++retries >= maxRetries) throw;
                _logger.LogError(ex, "Error while ensuring database is created. Trying again in 3 seconds...");
                Thread.Sleep(3000);
            }
        }
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

        modelBuilder.Entity<Aircraft>()
            .HasMany(x => x.FlightBookEntries)
            .WithOne(x => x.Aircraft)
            .HasForeignKey(x => x.AircraftId);

        modelBuilder.Entity<FlightbookEntry>()
            .HasOne(e => e.TowFlightEntry)
            .WithMany()
            .HasForeignKey(e => e.TowFlightEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GliderModel>()
            .HasIndex(x => x.Model)
            .IsUnique();

        // Seeds
        modelBuilder.Entity<KnownAircraft>().HasData(KnownAircraftSeeds.GetKnownAircraftSeeds());
    }

    private void InitializeGliderModelsFromJson()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "gliderModels.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning($"Glider model JSON file not found: {jsonPath}");
            return;
        }

        var jsonContent = File.ReadAllText(jsonPath);
        var modelsFromJson = JsonSerializer.Deserialize<List<GliderModel>>(jsonContent);
        if (modelsFromJson == null)
        {
            _logger.LogWarning("No glider models found in JSON.");
            return;
        }

        var dbModels = GliderModel.ToList();
        foreach (var jsonModel in modelsFromJson)
        {
            var existing = dbModels.FirstOrDefault(m => m.Model == jsonModel.Model);

            if (existing == null)
            {
                GliderModel.Add(new GliderModel
                {
                    Model = jsonModel.Model,
                    SelfLaunch = jsonModel.SelfLaunch
                });
            }
            else if (existing.SelfLaunch != jsonModel.SelfLaunch)
            {
                existing.SelfLaunch = jsonModel.SelfLaunch;
            }
        }
        SaveChanges();
    }
}
