using FlugsportWienOgn.Database.Entities;
using FlugsportWienOgn.Database;
using Microsoft.EntityFrameworkCore;
using FlugsportWienOgnApi.Models.Core;
using System.Collections.Concurrent;
using System.Threading;
using FlugsportWienOgnApi.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace FlugsportWienOgnApi.Services;

public class FlightbookService : BackgroundService
{
    // Thresholds and constants
    private const string AirfieldIcao = "LOXN";
    private const int GroundHeight = 284; // LOXN Platzhöhe in m
    private const int SpeedThreshold = 50; // km/h
    private const int AltitudeThreshold = 80; // m
    private const int TimeThreshold = 600; // s

    private const int CheckLaunchTypeDelay = 30; // s - Delay after takeoff before launch type is determined
    private const int WinchVerticalSpeedThreshold = 6; // m/s - Min average vertical speed in first 30 sec after takeoff to be interpreted as a winch launch
    private const int AerotowDetectMaxDistance = 150; // m - Max distance between glider and tow plane during takeoff phase
    private const int AerotowDetectTimeDifference = 30; // s - Max timespan between glider and tow plane takeoff in order to count as aerotow

    // LOXN Bounding Box
    private const double minLat = 47.825;
    private const double maxLat = 47.85;
    private const double minLng = 16.2;
    private const double maxLng = 16.24;

    private readonly ILogger<FlightbookService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly LiveTrackingService _liveTrackingService;
    private readonly KnownAircraftService _knownAircraftService;
    private readonly ConcurrentQueue<(int AircraftId, DateTime TakeoffTime)> _pendingLaunchTypeQueue = new();

    public FlightbookService(IServiceProvider serviceProvider, ILogger<FlightbookService> logger, LiveTrackingService liveTrackingService, KnownAircraftService knownAircraftService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _liveTrackingService = liveTrackingService;
        _knownAircraftService = knownAircraftService;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _liveTrackingService.FlightDataAdded += async aircraftId =>
        {
            await EvaluateFlightAsync(aircraftId);
        };
        _ = Task.Run(async () => await ProcessPendingLaunchTypes(cancellationToken), cancellationToken);

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }

    public async Task<IEnumerable<DepartureListItem>> GetLoxnFlightbook(bool? knownGlidersOnly)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

        var today = DateTime.UtcNow.Date;
        var flightbookEntries = await dbContext.FlightbookEntry
            .Include(e => e.Aircraft)
            .Where(e => e.AirfieldIcao == "LOXN" && e.TakeOffTimestamp != null && e.TakeOffTimestamp.Value.Date == today)
            .OrderByDescending(e => e.TakeOffTimestamp)
            .ToListAsync();

        var filteredEntries = knownGlidersOnly == true
            ? flightbookEntries.Where(e => _knownAircraftService.AllKnownPlaneFlarmIds.Contains(e.Aircraft.FlarmId))
            : flightbookEntries;

        var departureList = filteredEntries.Select(e =>
        {
            var known = _knownAircraftService.AllKnownPlanes.FirstOrDefault(p => p.FlarmId == e.Aircraft.FlarmId);
            return new DepartureListItem
            {
                FlarmId = e.Aircraft.FlarmId,
                Registration = e.Aircraft.Registration,
                RegistrationShort = e.Aircraft.CallSign,
                Model = e.Aircraft.Model,
                DepartureTimestamp = e.TakeOffTimestamp,
                LandingTimestamp = e.LandingTimestamp,
                LaunchType = (LaunchType)e.LaunchType,
                LaunchHeight = e.LaunchHeight
            };
        });
        return departureList;
    }

    public async Task EvaluateFlightAsync(int aircraftId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

        var buffer = await dbContext.FlightData
            .Where(x => x.AircraftId == aircraftId)
            .OrderByDescending(x => x.Timestamp)
            .Take(4)
            .ToListAsync();

        if (buffer.Count < 4) return;
        buffer.Reverse(); // oldest to newest

        var oldest = buffer[0];
        var newest = buffer[3];

        if (!IsCoordinateNearAirfield(newest.Latitude, newest.Longitude)) return;

        var isSlowBefore = buffer[0].Speed < SpeedThreshold && buffer[1].Speed < SpeedThreshold;
        var isFastAfter = buffer[2].Speed >= SpeedThreshold && buffer[3].Speed >= SpeedThreshold;

        var isFastBefore = buffer[0].Speed >= SpeedThreshold && buffer[1].Speed >= SpeedThreshold;
        var isSlowAfter = buffer[2].Speed < SpeedThreshold && buffer[3].Speed < SpeedThreshold;

        var timeSpan = (newest.Timestamp - oldest.Timestamp).TotalSeconds;
        if (timeSpan > TimeThreshold) return;

        // Prüfe "near ground" auf dem letzten langsamen Punkt vor dem Start
        var altBeforeStart = (buffer[1].Speed < SpeedThreshold) ? buffer[1].Altitude : buffer[0].Altitude;
        var altDiff = Math.Abs(altBeforeStart - GroundHeight);
        var nearGround = altDiff < AltitudeThreshold;

        var entry = await dbContext.FlightbookEntry.OrderBy(x => x.Id).LastOrDefaultAsync(x => x.AircraftId == aircraftId && x.LandingTimestamp == null);

        if (isSlowBefore && isFastAfter && nearGround)
        {
            if (entry == null)
            {
                _logger.LogInformation($"[Flightbook] Start erkannt für Aircraft {aircraftId} @ {newest.Timestamp:HH:mm:ss}");
                var newEntry = new FlightbookEntry
                {
                    AircraftId = aircraftId,
                    TakeOffTimestamp = newest.Timestamp,
                    AirfieldIcao = AirfieldIcao,
                    LaunchType = (int)LaunchType.Unknown
                };
                dbContext.FlightbookEntry.Add(newEntry);
                await dbContext.SaveChangesAsync();

                // Startart-Erkennung in Warteschlange stellen
                _pendingLaunchTypeQueue.Enqueue((aircraftId, newest.Timestamp));
            }
        }
        else if (isFastBefore && isSlowAfter && nearGround)
        {
            if (entry != null && entry.TakeOffTimestamp.HasValue)
            {
                _logger.LogInformation($"[Flightbook] Landung erkannt für Aircraft {aircraftId} @ {newest.Timestamp:HH:mm:ss}");
                entry.LandingTimestamp = newest.Timestamp;
                await dbContext.SaveChangesAsync();
            }
        }
    }

    private async Task ProcessPendingLaunchTypes(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            while (_pendingLaunchTypeQueue.TryDequeue(out var item))
            {
                _logger.LogInformation($"[Flightbook] Startartüberprüfung gequeued: {item.AircraftId} @ {item.TakeoffTime:HH:mm:ss}");
                var (aircraftId, takeoffTime) = item;

                // Verzögerung: z. B. 30 sek warten, damit genug Daten vorhanden sind
                var waitUntil = takeoffTime.AddSeconds(CheckLaunchTypeDelay);
                var delay = waitUntil - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation($"[Flightbook] Überprüfung wird durchgeführt in {delay}");
                    await Task.Delay(delay, cancellationToken);
                }

                try
                {
                    await DetermineAndStoreLaunchTypeAsync(aircraftId, takeoffTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Flightbook] Fehler bei LaunchType-Erkennung für Aircraft {aircraftId}");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); // Polling-Intervall
        }
        _logger.LogWarning($"[Flightbook] Startartüberprüfungsloop beendet... Es werden keine Startartüberprüfungen mehr durchgeführt");
    }

    private async Task DetermineAndStoreLaunchTypeAsync(int aircraftId, DateTime takeoffTime)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

        var entry = await dbContext.FlightbookEntry
            .Include(e => e.Aircraft)
            .FirstOrDefaultAsync(e => e.AircraftId == aircraftId && e.TakeOffTimestamp == takeoffTime);
        if (entry == null || entry.Aircraft == null)
        {
            _logger.LogWarning($"[Flightbook] Bei Startartüberprüfung kein Eintrag gefunden für AircraftId {aircraftId} und TakeoffTime {takeoffTime}");
            return;
        }
        if (entry.Aircraft.AircraftType != (int)AircraftType.Glider)
        {
            entry.LaunchType = (int)LaunchType.Motorized;
            await dbContext.SaveChangesAsync();
            return;
        }

        var data = await dbContext.FlightData
            .Where(x => x.AircraftId == aircraftId && x.Timestamp >= takeoffTime)
            .OrderBy(x => x.Timestamp)
            .ToListAsync();

        var avgVario = data.Average(x => x.VerticalSpeed);
        if (avgVario > WinchVerticalSpeedThreshold)
        {
            _logger.LogInformation($"[Flightbook] Startart erkannt als Windenstart (AvgVario: {avgVario}) für Aircraft {aircraftId} @ {takeoffTime:HH:mm:ss}");
            entry.LaunchType = (int)LaunchType.Winch;
            await dbContext.SaveChangesAsync();
            return;
        }

        // --- Tow Detection ---
        var towTakeoffs = await dbContext.FlightbookEntry
            .Include(f => f.Aircraft)
            .Where(f => f.TakeOffTimestamp >= takeoffTime.AddSeconds(AerotowDetectTimeDifference * -1)
                     && f.TakeOffTimestamp <= takeoffTime.AddSeconds(AerotowDetectTimeDifference)
                     && f.AircraftId != aircraftId
                     && f.Aircraft.AircraftType != (int)AircraftType.Glider)
            .ToListAsync();

        foreach (var tow in towTakeoffs)
        {
            var towData = await dbContext.FlightData
                .Where(fd => fd.AircraftId == tow.AircraftId && fd.Timestamp >= tow.TakeOffTimestamp)
                .OrderBy(fd => fd.Timestamp)
                .ToListAsync();

            if (towData.Count == 0) continue;

            var joined = data.Join(towData,
                glider => glider.Timestamp,
                towPoint => towPoint.Timestamp,
                (glider, towPoint) => new { glider, towPoint })
                .Take(10)
                .ToList();

            var countClose = joined.Count(pair => EarthDistanceCalculator.CalculateHaversineDistance(pair.glider.Latitude, pair.glider.Longitude, pair.towPoint.Latitude, pair.towPoint.Longitude) < AerotowDetectMaxDistance);

            if (countClose > 4)
            {
                entry.LaunchType = (int)LaunchType.Aerotow;
                _logger.LogInformation($"[Flightbook] F-Schlepp erkannt für Aircraft {aircraftId} durch {tow.AircraftId} @ {takeoffTime:HH:mm:ss}");
                await dbContext.SaveChangesAsync();
                return;
            }
            else if (countClose > 0)
            {
                _logger.LogInformation($"[Flightbook] F-Schlepp nicht ausreichend erkannt für Aircraft {aircraftId} durch {tow.AircraftId} @ {takeoffTime:HH:mm:ss}, countClose war {countClose}");
            }
        }
        entry.LaunchType = (int)LaunchType.Motorized;
        _logger.LogInformation($"[Flightbook] Startart erkannt als Eigenstart (kein Fschlepp gefunden) für Aircraft {aircraftId} @ {takeoffTime:HH:mm:ss} (AvgVario: {avgVario})");
        await dbContext.SaveChangesAsync();
    }

    private static bool IsCoordinateNearAirfield(double lat, double lng)
    {
        return lat >= minLat && lat <= maxLat && lng >= minLng && lng <= maxLng;
    }
}
