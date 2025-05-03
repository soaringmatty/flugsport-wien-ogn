using Aprs;
using FlugsportWienOgnApi.Models.Core;
using System.Threading;

namespace FlugsportWienOgnApi.Services;

public class LiveTrackingBackgroundService : BackgroundService
{
    private readonly LiveGliderService _liveGliderService;
    private readonly LiveTrackingService _tracker;
    private readonly AircraftProvider _aircraftProvider;
    private readonly ILogger _logger;

    public LiveTrackingBackgroundService(
        AircraftProvider aircraftProvider,
        LiveGliderService liveGliderService,
        LiveTrackingService tracker,
        ILogger<LiveTrackingBackgroundService> logger)
    {
        _aircraftProvider = aircraftProvider;
        _liveGliderService = liveGliderService;
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
        _logger.LogInformation("LiveGliderService started.");

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
}
