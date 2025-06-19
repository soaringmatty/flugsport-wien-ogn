using FlugsportWienOgn.Database.Entities;
using FlugsportWienOgn.Database;
using Microsoft.EntityFrameworkCore;
using FlugsportWienOgnApi.Models.Core;
using System.Collections.Concurrent;
using FlugsportWienOgnApi.Utils;
using Aircraft = FlugsportWienOgn.Database.Entities.Aircraft;
using Aprs;

namespace FlugsportWienOgnApi.Services;

public class FlightbookBackgroundService : BackgroundService
{
    // Thresholds and constants
    private const int SpeedThreshold = 50; // km/h
    private const int AltitudeThreshold = 80; // m
    private const int TimeThreshold = 600; // sec

    private const int CheckLaunchTypeDelay = 30; // s - Delay after takeoff before launch type is determined
    private const int WinchVerticalSpeedThreshold = 6; // m/s - Min average vertical speed in first 30 sec after takeoff to be interpreted as a winch launch
    private const int AerotowDetectMaxDistanceBetweenFlightPaths = 70; // m - Max distance between glider and tow plane during takeoff phase
    private const double AerotowDetectMinMatchRatio = 0.6; // Min required factor of the flight datapoints that have to match between glider and tow plane to count as aerotow
    private const int AerotowDetectTimeDifference = 30; // sec - Max timespan between glider and tow plane takeoff in order to count as aerotow

    // Old check consts
    private const int LaunchHeightCourseChangeThreshold = 30;
    private const int LaunchHeightCourseChangeWindow = 3;

    // New check consts
    private const int LaunchHeightCheckIntervalTow = 45; // sec - Interval for the launch height checks for aerotow
    private const int LaunchHeightCheckIntervalWinch = 10; // sec - Interval for the launch height checks for winch launch
    private const int LaunchHeightCheckTimeoutTow = 60; // min - Max duration of an aerotow (after that, launch height check is stopped)
    private const int LaunchHeightCheckTimeoutWinch = 120; // sec - Max duration of a winch launch (after that, launch height check is stopped)
    private const int LaunchHeightWinchCourseChangeThreshold = 60; // ° - Glider course change compared to takeoff course that declares the winch launch to be finished 
    private const int LaunchHeightPeakWindowWinch = 2;
    private const int LaunchHeightPeakWindowTow = 5;
    private const double LaunchHeightTurningThresholdPerSecond = 15.0; // Min turn rate (degrees per second) to count as thermaling
    private const int LaunchHeightTurningMinDuration = 10; // sec - Min duration of the the circling to count as thermaling

    private readonly AirfieldConfig _airfield;
    private readonly ILogger<FlightbookBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly LiveTrackingService _liveTrackingService;
    private readonly KnownAircraftService _knownAircraftService;
    private readonly AircraftProvider _aircraftProvider;
    private readonly ConcurrentQueue<(Aircraft aircraft, DateTime takeoffTime)> _pendingLaunchTypeQueue = new();

    public FlightbookBackgroundService(AirfieldConfig airfield, IServiceProvider serviceProvider, ILogger<FlightbookBackgroundService> logger, LiveTrackingService liveTrackingService, KnownAircraftService knownAircraftService, AircraftProvider aircraftProvider)
    {
        _airfield = airfield;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _liveTrackingService = liveTrackingService;
        _knownAircraftService = knownAircraftService;
        _aircraftProvider = aircraftProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _liveTrackingService.FlightDataAdded += async aircraft =>
        {
            await EvaluateFlightAsync(aircraft);
        };
        _ = Task.Run(async () => await ProcessPendingLaunchTypes(cancellationToken), cancellationToken);
        _logger.LogInformation($"FlightbookService started ({_airfield.Icao}) - departures and landings are beeing tracked");

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }

    public async Task EvaluateFlightAsync(Aircraft aircraft)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

        var buffer = await dbContext.FlightData
            .Where(x => x.AircraftId == aircraft.Id)
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

        var altBeforeStart = (buffer[1].Speed < SpeedThreshold) ? buffer[1].Altitude : buffer[0].Altitude;
        var nearGround = Math.Abs(altBeforeStart - _airfield.GroundHeight) < AltitudeThreshold;

        var entry = await dbContext.FlightbookEntry.OrderBy(x => x.Id)
            .LastOrDefaultAsync(x => x.AircraftId == aircraft.Id && x.LandingTimestamp == null);

        if (isSlowBefore && isFastAfter && nearGround)
        {
            if (entry == null)
            {
                var aircraftType = (AircraftType)aircraft.AircraftType;
                var aircraftTypeString = aircraftType == AircraftType.Glider ? "(Segelflug)" : "(Motorflug)";
                _logger.LogInformation($"[Flightbook] Start erkannt von {aircraft.CallSign} {aircraftTypeString} um {newest.Timestamp:HH:mm:ss}");
                var newEntry = new FlightbookEntry
                {
                    AircraftId = aircraft.Id,
                    TakeOffTimestamp = newest.Timestamp,
                    AirfieldIcao = _airfield.Icao,
                    LaunchType = aircraftType == AircraftType.Glider ? (int)LaunchType.Unknown : (int)LaunchType.Motorized
                };
                dbContext.FlightbookEntry.Add(newEntry);
                await dbContext.SaveChangesAsync();

                if (aircraftType == AircraftType.Glider)
                {
                    _pendingLaunchTypeQueue.Enqueue((aircraft, newest.Timestamp));
                }
            }
        }
        else if (isFastBefore && isSlowAfter && nearGround)
        {
            if (entry != null && entry.TakeOffTimestamp.HasValue)
            {
                _logger.LogInformation($"[Flightbook] Landung erkannt von {aircraft.CallSign} um {newest.Timestamp:HH:mm:ss}");
                entry.LandingTimestamp = newest.Timestamp;
                await dbContext.SaveChangesAsync();
            }
            else if (entry == null)
            {
                _logger.LogInformation($"[Flightbook] Landung erkannt von {aircraft.CallSign} um {newest.Timestamp:HH:mm:ss} - kein Start vorhanden");

                var newLandingEntry = new FlightbookEntry
                {
                    AircraftId = aircraft.Id,
                    LandingTimestamp = newest.Timestamp,
                    AirfieldIcao = _airfield.Icao,
                    LaunchType = (int)LaunchType.Unknown
                };
                dbContext.FlightbookEntry.Add(newLandingEntry);
                await dbContext.SaveChangesAsync();
            }
        }
    }

    private bool IsCoordinateNearAirfield(double lat, double lng)
    {
        return lat >= _airfield.MinLat && lat <= _airfield.MaxLat && lng >= _airfield.MinLng && lng <= _airfield.MaxLng;
    }

    private async Task ProcessPendingLaunchTypes(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            while (_pendingLaunchTypeQueue.TryDequeue(out var item))
            {
                // Wait until 30 seconds after takeoff
                var (aircraft, takeoffTime) = item;
                var delay = takeoffTime.AddSeconds(CheckLaunchTypeDelay) - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }

                try
                {
                    await DetermineAndStoreLaunchTypeAsync(aircraft, takeoffTime, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Flightbook] Fehler bei Startarterkennung von {aircraft.CallSign}");
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
        _logger.LogWarning("[Flightbook] Startart-Überprüfungsloop beendet.");
    }

    private async Task DetermineAndStoreLaunchTypeAsync(Aircraft aircraft, DateTime takeoffTime, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

        var entry = await dbContext.FlightbookEntry
            .Include(e => e.Aircraft)
            .FirstOrDefaultAsync(e => e.AircraftId == aircraft.Id && e.TakeOffTimestamp == takeoffTime);
        if (entry == null || entry.Aircraft == null)
        {
            _logger.LogWarning($"[Flightbook] Bei Startartüberprüfung kein Eintrag gefunden für {aircraft.CallSign} und Startzeit {takeoffTime}");
            return;
        }
        if ((AircraftType)entry.Aircraft.AircraftType != AircraftType.Glider)
        {
            return;
        }

        var flightData = await dbContext.FlightData
            .Where(x => x.AircraftId == aircraft.Id && x.Timestamp >= takeoffTime)
            .OrderBy(x => x.Timestamp)
            .ToListAsync();

        if (flightData.Count < 3)
        {
            _logger.LogWarning($"[Flightbook] Nicht genug Daten für Startarterkennung bei {aircraft.CallSign}");
            return;
        }

        var avgClimbRate = flightData.Average(f => f.VerticalSpeed);
        if (avgClimbRate > WinchVerticalSpeedThreshold)
        {
            entry.LaunchType = (int)LaunchType.Winch;
            await dbContext.SaveChangesAsync();
            _logger.LogInformation($"[Flightbook] Startart: Windenstart (AvgVario: {avgClimbRate:F1}) von {aircraft.CallSign} um {takeoffTime:HH:mm:ss}");
            StartLaunchHeightTracking(entry, cancellationToken);
            return;
        }

        var isAerotow = await CheckForAerotowAsync(aircraft, entry, dbContext);
        if (isAerotow)
        {
            StartLaunchHeightTracking(entry, cancellationToken);
            return;
        }

        // Check self launch capability -> if model is unknown -> defaults to NO (has no self launch capability)
        var gliderModel = await dbContext.GliderModel.Where(x => x.Model == aircraft.Model.Trim()).FirstOrDefaultAsync();
        var canSelfLaunch = gliderModel != null ? gliderModel.SelfLaunch : false;
        if (canSelfLaunch.HasValue && canSelfLaunch.Value)
        {
            entry.LaunchType = (int)LaunchType.Motorized;
            await dbContext.SaveChangesAsync();
            _logger.LogInformation($"[Flightbook] Startart: Eigenstart von {aircraft.CallSign} ({aircraft.Model})");
            return;
        }

        entry.LaunchType = (int)LaunchType.Unknown;
        await dbContext.SaveChangesAsync();
        _logger.LogInformation($"[Flightbook] Startart: Unbekannt von {aircraft.CallSign}");
    }

    private async Task<bool> CheckForAerotowAsync(Aircraft aircraft, FlightbookEntry flightbookEntry, FlightDbContext dbContext)
    {
        if (!flightbookEntry.TakeOffTimestamp.HasValue) return false;

        var takeoffTime = flightbookEntry.TakeOffTimestamp.Value;
        var gliderFlightData = await dbContext.FlightData
            .Where(x => x.AircraftId == aircraft.Id && x.Timestamp >= takeoffTime)
            .OrderBy(x => x.Timestamp)
            .ToListAsync();

        var towTakeoffs = await dbContext.FlightbookEntry
            .Include(f => f.Aircraft)
            .Where(f => f.TakeOffTimestamp >= takeoffTime.AddSeconds(AerotowDetectTimeDifference * -1)
                     && f.TakeOffTimestamp <= takeoffTime.AddSeconds(AerotowDetectTimeDifference)
                     && f.AircraftId != aircraft.Id
                     && f.Aircraft.AircraftType != (int)AircraftType.Glider)
        .ToListAsync();

        _logger.LogInformation($"[Flightbook] F-Schlepp Überprüfung für {aircraft.CallSign} -> zwischen {takeoffTime.AddSeconds(AerotowDetectTimeDifference * -1):HH:mm:ss} und {takeoffTime.AddSeconds(AerotowDetectTimeDifference):HH:mm:ss} wurden {towTakeoffs.Count} andere Starts gefunden");

        foreach (var tow in towTakeoffs)
        {
            var towFlightData = await dbContext.FlightData
                .Where(fd => fd.AircraftId == tow.AircraftId && fd.Timestamp >= tow.TakeOffTimestamp)
                .OrderBy(fd => fd.Timestamp)
                .ToListAsync();
            var pathMatchCount = CountPathSimilaritiesByPathSegmenting(gliderFlightData, towFlightData);
            _logger.LogInformation($"[Flightbook] F-Schlepp Überprüfung für {aircraft.CallSign} -> {tow.Aircraft.CallSign} hat {pathMatchCount}/{towFlightData.Count} Datenpunkte in der Nähe vom Segelflugzeug");
            double matchRatioPathSegementing = (double)pathMatchCount / gliderFlightData.Count;
            if (matchRatioPathSegementing >= AerotowDetectMinMatchRatio)
            {
                flightbookEntry.LaunchType = (int)LaunchType.Aerotow;
                flightbookEntry.TowFlightEntryId = tow.Id;
                await dbContext.SaveChangesAsync();
                await dbContext.Entry(flightbookEntry)
                    .Reference(e => e.TowFlightEntry)
                    .LoadAsync();
                _logger.LogInformation($"[Flightbook] Startart: F-Schlepp von {aircraft.CallSign} durch {tow.Aircraft.CallSign} um {takeoffTime:HH:mm:ss}");
                return true;
            }
        }
        return false;
    }

    private int CountPathSimilaritiesByPathSegmenting(List<FlightPathItem> gliderPath, List<FlightPathItem> towPath)
    {
        if (gliderPath.Count == 0 || towPath.Count < 2)
            return 0;

        int matchCount = 0;

        foreach (var gPoint in gliderPath)
        {
            double minDistance = double.MaxValue;

            for (int i = 0; i < towPath.Count - 1; i++)
            {
                var tStart = towPath[i];
                var tEnd = towPath[i + 1];

                double dist = EarthDistanceCalculator.DistancePointToSegment(
                    new Coordinate(gPoint.Latitude, gPoint.Longitude),
                    new Coordinate(tStart.Latitude, tStart.Longitude),
                    new Coordinate(tEnd.Latitude, tEnd.Longitude));
                if (dist < minDistance)
                    minDistance = dist;
            }

            if (minDistance < AerotowDetectMaxDistanceBetweenFlightPaths)
                matchCount++;
        }
        return matchCount;
    }

    private void StartLaunchHeightTracking(FlightbookEntry flightbookEntry, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var launchType = (LaunchType)flightbookEntry.LaunchType;
                if (launchType == LaunchType.Winch) await TrackLaunchHeightWinchAsync(flightbookEntry);
                else if (launchType == LaunchType.Aerotow) await TrackLaunchHeightAerotowAsync(flightbookEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Flightbook] Fehler beim Erkennen der Starthöhe von {flightbookEntry.Aircraft.CallSign}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Continously check if winch launch is finished and save launch altitude in database when that is the case
    /// </summary>
    /// <param name="flightbookEntry"></param>
    /// <returns></returns>
    private async Task TrackLaunchHeightWinchAsync(FlightbookEntry flightbookEntry)
    {
        var startTime = DateTime.UtcNow;
        int lastPeak = 0;

        // Check every 10 seconds for a max time span of 120 seconds after takeoff
        while ((DateTime.UtcNow - startTime).TotalSeconds <= LaunchHeightCheckTimeoutWinch)
        {
            await Task.Delay(TimeSpan.FromSeconds(LaunchHeightCheckIntervalWinch));

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
            var gliderFlightData = await db.FlightData
                .Where(x => x.AircraftId == flightbookEntry.Aircraft.Id && x.Timestamp >= flightbookEntry.TakeOffTimestamp)
                .OrderBy(x => x.Timestamp)
                .ToListAsync();

            _logger.LogInformation($"[Flightbook] Starthöhe wird überprüft für {flightbookEntry.Aircraft.CallSign} (Windenstart)");
            if (gliderFlightData.Count < 3)
            {
                _logger.LogInformation($"[Flightbook] Nur {gliderFlightData.Count} Datenpunkte von {flightbookEntry.Aircraft.CallSign} -> wird übersprungn");
                continue;
            }

            // Check for winch launch top altitude
            var result = DetectWinchLaunchReleaseHeight(gliderFlightData, ref lastPeak);
            if (result.HasValue)
            {
                var entry = await db.FlightbookEntry.FirstOrDefaultAsync(e => e.AircraftId == flightbookEntry.Aircraft.Id && e.TakeOffTimestamp == flightbookEntry.TakeOffTimestamp);
                if (entry != null)
                {
                    entry.LaunchHeight = (int)(Math.Round((result.Value - _airfield.GroundHeight) / 10.0) * 10);
                    await db.SaveChangesAsync();
                    _logger.LogInformation($"[Flightbook] Starthöhe erkannt: {result.Value}m ({entry.LaunchHeight}m AGL) für {flightbookEntry.Aircraft.CallSign} (Windenstart)");
                    return;
                }
            }
        }

        if (lastPeak > 0)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
            var entry = await db.FlightbookEntry.FirstOrDefaultAsync(e => e.AircraftId == flightbookEntry.Aircraft.Id && e.TakeOffTimestamp == flightbookEntry.TakeOffTimestamp);
            if (entry != null)
            {
                entry.LaunchHeight = (int)(Math.Round((lastPeak - _airfield.GroundHeight) / 10.0) * 10);
                await db.SaveChangesAsync();
                _logger.LogInformation($"[Flightbook] Starthöhe Timeout-Fallback: letzte bekannte Höhe {entry.LaunchHeight}m für {flightbookEntry.Aircraft.CallSign} (Windenstart)");
            }
        }
    }

    /// <summary>
    /// Continously check if aerotow is finished and save tow altitude in database when that is the case
    /// </summary>
    /// <param name="flightbookEntry"></param>
    /// <returns></returns>
    private async Task TrackLaunchHeightAerotowAsync(FlightbookEntry flightbookEntry)
    {
        var startTime = DateTime.UtcNow;
        int lastPeak = 0;

        // Check every 60 seconds for a max time span of 60 minutes after takeoff
        while ((DateTime.UtcNow - startTime).TotalMinutes <= LaunchHeightCheckTimeoutTow)
        {
            await Task.Delay(TimeSpan.FromSeconds(LaunchHeightCheckIntervalTow));

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

            var timestampOfLastCheck = DateTime.UtcNow.AddSeconds(LaunchHeightCheckIntervalTow * -1);
            var gliderFlightData = await db.FlightData
                .Where(x => x.AircraftId == flightbookEntry.Aircraft.Id && x.Timestamp >= flightbookEntry.TakeOffTimestamp && x.Timestamp >= timestampOfLastCheck)
                .OrderBy(x => x.Timestamp)
                .ToListAsync();

            //_logger.LogInformation($"[Flightbook] Starthöhe wird überprüft für {flightbookEntry.Aircraft.CallSign} (F-Schlepp)");
            if (gliderFlightData.Count < 3)
            {
                _logger.LogInformation($"[Flightbook] Nur {gliderFlightData.Count} Datenpunkte von {flightbookEntry.Aircraft.CallSign} -> wird übersprungn");
                continue;
            }

            int? result = null;
            var towFlight = flightbookEntry.TowFlightEntry;
            if (towFlight == null)
            {
                _logger.LogWarning($"[Flightbook] Starthöhe überprüfen nicht möglich (F-Schlepp) -> Verweis zu Schleppflugzeug Start für {flightbookEntry.Aircraft.CallSign}");
                return;
            }

            // Check if glider path is still similar to tow plane path
            // Get tow plane flight data from last 30 seconds
            var towData = await db.FlightData
                .Where(fd => fd.AircraftId == towFlight.AircraftId && fd.Timestamp >= towFlight.TakeOffTimestamp && fd.Timestamp >= timestampOfLastCheck)
                .OrderBy(fd => fd.Timestamp)
                .ToListAsync();

            // Min of 3 data points -> else fallback checks
            if (gliderFlightData.Count >= 3 && towData.Count >= 3)
            {
                var matchCount = CountPathSimilaritiesByPathSegmenting(gliderFlightData, towData);
                _logger.LogInformation($"[Flightbook] F-Schlepp Überprüfung für {flightbookEntry.Aircraft.CallSign} -> {towFlight.Aircraft.CallSign} hat {matchCount}/{towData.Count} Datenpunkte in der Nähe vom Segelflugzeug");
                double matchRatio = (double)matchCount / gliderFlightData.Count;
                // If flight path does not match anymore -> tow is finished
                if (matchRatio < AerotowDetectMinMatchRatio)
                {
                    result = lastPeak;
                }
                else
                {
                    lastPeak = gliderFlightData.Max(x => x.Altitude);
                }
            }
            // If not enough data points from tow plane -> fallback checks
            else
            {
                _logger.LogInformation($"[Flightbook] F-Schlepp Überprüfung für {flightbookEntry.Aircraft.CallSign} -> Nicht genug Datenpunkte... alternative Checks (Motorflugzeug: {towData.Count}, Segelflugzeug: {gliderFlightData.Count})");
                result = DetectAerotowReleaseHeight(gliderFlightData, ref lastPeak);
            }

            if (result.HasValue)
            {
                var entry = await db.FlightbookEntry.FirstOrDefaultAsync(e => e.AircraftId == flightbookEntry.Aircraft.Id && e.TakeOffTimestamp == flightbookEntry.TakeOffTimestamp);
                if (entry != null)
                {
                    entry.LaunchHeight = (int)(Math.Round((result.Value - _airfield.GroundHeight) / 10.0) * 10);
                    await db.SaveChangesAsync();
                    _logger.LogInformation($"[Flightbook] Starthöhe erkannt: {result.Value}m ({entry.LaunchHeight}m AGL) für {flightbookEntry.Aircraft.CallSign} (F-Schlepp)");
                    return;
                }
            }
        }

        if (lastPeak > 0)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
            var entry = await db.FlightbookEntry.FirstOrDefaultAsync(e => e.AircraftId == flightbookEntry.Aircraft.Id && e.TakeOffTimestamp == flightbookEntry.TakeOffTimestamp);
            if (entry != null)
            {
                entry.LaunchHeight = (int)(Math.Round((lastPeak - _airfield.GroundHeight) / 10.0) * 10);
                await db.SaveChangesAsync();
                _logger.LogInformation($"[Flightbook] Starthöhe Timeout-Fallback: letzte bekannte Höhe {entry.LaunchHeight}m für {flightbookEntry.Aircraft.CallSign} (F-Schlepp)");
            }
        }
    }

    /// <summary>
    /// Tries to detect and return winch launch release altitude by checking for a peak in the altitude or a clear change in flight course
    /// </summary>
    /// <param name="data"></param>
    /// <param name="lastPeak"></param>
    /// <returns></returns>
    private int? DetectWinchLaunchReleaseHeight(List<FlightPathItem> data, ref int lastPeak)
    {
        int belowCount = 0;
        int lastPeakCurrentCycle = 0;
        double startCourse = data.First().Course;

        foreach (var point in data)
        {
            // If two concecutive flightPathItems are below last high point -> winch launch finished
            if (point.Altitude > lastPeakCurrentCycle)
            {
                lastPeakCurrentCycle = point.Altitude;
                belowCount = 0;
            }
            else
            {
                belowCount++;
                if (belowCount >= LaunchHeightPeakWindowWinch)
                {
                    _logger.LogInformation($"[Flightbook] Windenstart: Gipfel erkannt");
                    lastPeak = lastPeakCurrentCycle;
                    return lastPeak;
                }
            }

            // If glider makes a clear turn -> winch launch finished 
            double diff = Math.Abs(point.Course - startCourse);
            if (diff > 180) diff = 360 - diff;
            if (diff > LaunchHeightWinchCourseChangeThreshold)
            {
                _logger.LogInformation($"[Flightbook] Windenstart: Kursänderung um >60° erkannt");
                lastPeak = lastPeakCurrentCycle;
                return lastPeak;
            }
        }
        return null;
    }

    /// <summary>
    /// Tries to detect and return aerotow release altitude by checking for a peak in the altitude or thermaling of the glider
    /// </summary>
    /// <param name="data"></param>
    /// <param name="lastPeak"></param>
    /// <returns></returns>
    private int? DetectAerotowReleaseHeight(List<FlightPathItem> data, ref int lastPeak)
    {
        int belowCount = 0;
        int startIndex = -1;
        double totalChange = 0;
        double totalTime = 0;

        for (int i = 1; i < data.Count; i++)
        {
            var prev = data[i - 1];
            var curr = data[i];

            // If five concecutive flightPathItems are below last high point -> winch launch finished
            if (curr.Altitude > lastPeak)
            {
                lastPeak = curr.Altitude;
                belowCount = 0;
            }
            else
            {
                belowCount++;
                if (belowCount >= LaunchHeightPeakWindowTow)
                {
                    _logger.LogInformation($"[Flightbook] F-Schlepp: Gipfel erkannt");
                    return lastPeak;
                }
            }

            // If glider starts thermaling (indicated by min turn rate per second for at least 10 seconds)
            double delta = Math.Abs(curr.Course - prev.Course);
            if (delta > 180) delta = 360 - delta;
            double dt = (curr.Timestamp - prev.Timestamp).TotalSeconds;
            if (dt < 1 || dt > 20) continue;
            double rate = delta / dt;

            if (rate >= LaunchHeightTurningThresholdPerSecond)
            {
                if (startIndex == -1) startIndex = i - 1;
                totalChange += delta;
                totalTime += dt;

                if (totalTime >= LaunchHeightTurningMinDuration)
                {
                    int result = data[startIndex].Altitude;
                    _logger.LogInformation($"[Flightbook] F-Schlepp: Thermikkreis");
                    return result;
                }
            }
            else
            {
                startIndex = -1;
                totalChange = 0;
                totalTime = 0;
            }
        }
        return null;
    }
}