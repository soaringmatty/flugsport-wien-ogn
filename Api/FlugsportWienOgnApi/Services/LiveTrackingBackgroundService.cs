using Aprs;
using FlugsportWienOgn.Database;
using FlugsportWienOgn.Database.Entities;
using FlugsportWienOgnApi.Models.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace FlugsportWienOgnApi.Services;

public class LiveTrackingBackgroundService : BackgroundService
{
    private readonly LiveGliderService _liveGliderService;
    private readonly LiveTrackingService _tracker;
    private readonly AircraftProvider _aircraftProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public LiveTrackingBackgroundService(
        AircraftProvider aircraftProvider,
        LiveGliderService liveGliderService,
        LiveTrackingService tracker,
        IServiceProvider serviceProvider,
        ILogger<LiveTrackingBackgroundService> logger)
    {
        _aircraftProvider = aircraftProvider;
        _liveGliderService = liveGliderService;
        _serviceProvider = serviceProvider;
        _tracker = tracker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Download aircraft db
        try
        {
            _logger.LogInformation("Initializing AircraftProvider - downloading glidernet ddb...");
            await _aircraftProvider.InitializeAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("AircraftProvider initialized");
            await UpdateGliderModelsInDatabase(cancellationToken);
            
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AircraftProvider initialization failed.");
            return; // HostedService beendet sich, anstelle weiterzulaufen
        }

        // Event-Handler anmelden
        //_glider.OnDataReceived += _tracker.HandleFlightData;

        // Live-Stream starten
        _ = _tracker.StartFlushBufferLoop(cancellationToken);
        _liveGliderService.Start(cancellationToken);
        _logger.LogInformation("LiveGliderService started - All OGN flight data will be tracked now");

        // Block lifetype cycle until shutdown blockieren
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping LiveGliderService");
        //_liveGliderService.FlightDataReceived -= _tracker.HandleFlightData;
        _liveGliderService.Stop();
        await _liveGliderService.DisposeAsync().ConfigureAwait(false);
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateGliderModelsInDatabase(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

        var existingModels = await dbContext.GliderModel
            .Select(g => g.Model)
            .ToListAsync(cancellationToken);

        var newModels = _aircraftProvider.GetDistinctGliderModels()
            .Where(m => !existingModels.Contains(m))
            .Distinct()
            .ToList();

        var gliderModelEntities = newModels
            .Select(m => new GliderModel { Model = m, SelfLaunch = null })
            .ToList();

        if (gliderModelEntities.Any())
        {
            await dbContext.GliderModel.AddRangeAsync(gliderModelEntities, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"{gliderModelEntities.Count} new GliderModels have been added to database");
        }
        else
        {
            _logger.LogInformation($"GliderModels in database are up to date");
        }
    }
}
