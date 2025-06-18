using FlugsportWienOgn.Database.Entities;
using FlugsportWienOgn.Database;
using Microsoft.EntityFrameworkCore;
using FlugsportWienOgnApi.Models.Core;
using System.Collections.Concurrent;
using FlugsportWienOgnApi.Utils;
using Aircraft = FlugsportWienOgn.Database.Entities.Aircraft;

namespace FlugsportWienOgnApi.Services;

public class FlightbookService : BackgroundService
{

    // LOXN Airfield
    private const string AirfieldIcao = "LOXN";
    private const int GroundHeight = 284; // LOXN Platzhöhe in m
    private const double minLat = 47.825;
    private const double maxLat = 47.85;
    private const double minLng = 16.2;
    private const double maxLng = 16.24;

    // TMP TEST Airfield
    //private const string AirfieldIcao = "WOES";
    //private const int GroundHeight = 558; // LOXN Platzhöhe in m
    //private const double minLat = 47.726110;
    //private const double maxLat = 47.736588;
    //private const double minLng = 12.422553;
    //private const double maxLng = 12.451435;

    // Thresholds and constants
    private const int SpeedThreshold = 50; // km/h
    private const int AltitudeThreshold = 80; // m
    private const int TimeThreshold = 600; // s

    private const int CheckLaunchTypeDelay = 30; // s - Delay after takeoff before launch type is determined
    private const int WinchVerticalSpeedThreshold = 6; // m/s - Min average vertical speed in first 30 sec after takeoff to be interpreted as a winch launch
    private const int AerotowDetectMaxDistanceBetweenFlightPaths = 70; // m - Max distance between glider and tow plane during takeoff phase
    private const double AerotowDetectMinMatchRatio = 0.6; // Min required factor of the flight datapoints that have to match between glider and tow plane to count as aerotow
    private const int AerotowDetectTimeDifference = 30; // s - Max timespan between glider and tow plane takeoff in order to count as aerotow

    private const int LaunchHeightCheckDelay = 15; // s - Delay between launch height checks for an airplane
    private const int LaunchHeightPeakWindowSizeWinch = 2;
    private const int LaunchHeightPeakWindowSizeTow = 4;
    private const int LaunchHeightCourseChangeThreshold = 30;
    private const int LaunchHeightCourseChangeWindow = 3;
    private const double LaunchHeightTurningThresholdPerSecond = 15.0; // Grad pro Sekunde, grob schätzbar
    private const int LaunchHeightTurningMinDuration = 10; // Sek mindestens, die kurbelnde Bewegung dauert

    private readonly ILogger<FlightbookService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly LiveTrackingService _liveTrackingService;
    private readonly KnownAircraftService _knownAircraftService;
    private readonly ConcurrentQueue<(Aircraft aircraft, DateTime takeoffTime)> _pendingLaunchTypeQueue = new();
    private readonly ConcurrentQueue<(Aircraft aircraft, DateTime takeoffTime)> _pendingLaunchHeightQueue = new();

    public FlightbookService(IServiceProvider serviceProvider, ILogger<FlightbookService> logger, LiveTrackingService liveTrackingService, KnownAircraftService knownAircraftService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _liveTrackingService = liveTrackingService;
        _knownAircraftService = knownAircraftService;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _liveTrackingService.FlightDataAdded += async aircraft =>
        {
            await EvaluateFlightAsync(aircraft);
        };
        _ = Task.Run(async () => await ProcessPendingLaunchTypes(cancellationToken), cancellationToken);
        _ = Task.Run(async () => await ProcessPendingLaunchHeights(cancellationToken), cancellationToken);
        _logger.LogInformation("FlightbookService started - LOXN departures and landings are beeing tracked");

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

        // Prüfe "near ground" auf dem letzten langsamen Punkt vor dem Start
        var altBeforeStart = (buffer[1].Speed < SpeedThreshold) ? buffer[1].Altitude : buffer[0].Altitude;
        var altDiff = Math.Abs(altBeforeStart - GroundHeight);
        var nearGround = altDiff < AltitudeThreshold;

        var entry = await dbContext.FlightbookEntry.OrderBy(x => x.Id).LastOrDefaultAsync(x => x.AircraftId == aircraft.Id && x.LandingTimestamp == null);

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
                    AirfieldIcao = AirfieldIcao,
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
                    AirfieldIcao = AirfieldIcao,
                    LaunchType = (int)LaunchType.Unknown
                };
                dbContext.FlightbookEntry.Add(newLandingEntry);
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
                var (aircraft, takeoffTime) = item;

                // Verzögerung: z. B. 30 sek warten, damit genug Daten vorhanden sind
                var waitUntil = takeoffTime.AddSeconds(CheckLaunchTypeDelay);
                var delay = waitUntil - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    //_logger.LogInformation($"[Flightbook] Startart von {aircraft.CallSign} wird überprüft in {delay}");
                    await Task.Delay(delay, cancellationToken);
                }

                try
                {
                    await DetermineAndStoreLaunchTypeAsync(aircraft, takeoffTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Flightbook] Fehler bei Startarterkennung von {aircraft.CallSign}");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
        _logger.LogWarning($"[Flightbook] Startart-Überprüfungsloop beendet... Es werden keine Startartüberprüfungen mehr durchgeführt");
    }

    private async Task ProcessPendingLaunchHeights(CancellationToken cancellationToken)
    {
        var flarmIdsCheckedInCurrentIteration = new HashSet<string>();
        while (!cancellationToken.IsCancellationRequested)
        {
            flarmIdsCheckedInCurrentIteration.Clear();
            while (_pendingLaunchHeightQueue.TryDequeue(out var item))
            {
                try
                {
                    flarmIdsCheckedInCurrentIteration.TryGetValue(item.aircraft.FlarmId, out var value);
                    if (!string.IsNullOrEmpty(value)) {
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                    }

                    _logger.LogInformation($"[Flightbook] Prüfe Starthöhe von {item.aircraft.CallSign}...");
                    flarmIdsCheckedInCurrentIteration.Add(item.aircraft.FlarmId);
                    await DetectLaunchHeight(item.aircraft, item.takeoffTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Flightbook] Fehler bei Starthöhe checken von {item.aircraft.CallSign}");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
        _logger.LogWarning($"[Flightbook] Starthöhe-Überprüfungsloop beendet... Es werden keine Starthöhenüberprüfungen mehr durchgeführt");
    }

    private async Task DetermineAndStoreLaunchTypeAsync(Aircraft aircraft, DateTime takeoffTime)
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
        if (entry.Aircraft.AircraftType != (int)AircraftType.Glider)
        {
            entry.LaunchType = (int)LaunchType.Motorized;
            await dbContext.SaveChangesAsync();
            return;
        }

        var data = await dbContext.FlightData
            .Where(x => x.AircraftId == aircraft.Id && x.Timestamp >= takeoffTime)
            .OrderBy(x => x.Timestamp)
            .ToListAsync();

        var avgVario = data.Average(x => x.VerticalSpeed);
        if (avgVario > WinchVerticalSpeedThreshold)
        {
            _logger.LogInformation($"[Flightbook] Startart: Windenstart (AvgVario: {avgVario}) von {aircraft.CallSign} um {takeoffTime:HH:mm:ss}");
            entry.LaunchType = (int)LaunchType.Winch;
            await dbContext.SaveChangesAsync();
            _pendingLaunchHeightQueue.Enqueue((aircraft, takeoffTime));
            return;
        }

        var isAearotow = await CheckForAerotow(aircraft, entry, dbContext);
        if (isAearotow)
        {
            _pendingLaunchHeightQueue.Enqueue((aircraft, takeoffTime));
            return;
        }
        
        entry.LaunchType = (int)LaunchType.Motorized;
        _logger.LogInformation($"[Flightbook] Startart: Eigenstart (AvgVario: {avgVario}) von {aircraft.CallSign} um {takeoffTime:HH:mm:ss}");
        await dbContext.SaveChangesAsync();
    }

    private async Task<bool> CheckForAerotow(Aircraft aircraft, FlightbookEntry flightbookEntry, FlightDbContext dbContext)
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

                double dist = DistancePointToSegment(gPoint, tStart, tEnd);
                if (dist < minDistance)
                    minDistance = dist;
            }

            if (minDistance < AerotowDetectMaxDistanceBetweenFlightPaths)
                matchCount++;
        }
        return matchCount;
    }

    private static bool IsCoordinateNearAirfield(double lat, double lng)
    {
        return lat >= minLat && lat <= maxLat && lng >= minLng && lng <= maxLng;
    }

    private double DistancePointToSegment(FlightPathItem p, FlightPathItem a, FlightPathItem b)
    {
        // Konvertiere Längen- und Breitengrad zu kartesischem Raum (einfache Projektion)
        // Achtung: für größere Distanzen wäre dies ungenau – hier reicht's.
        double x = LonToX(p.Longitude);
        double y = LatToY(p.Latitude);
        double x1 = LonToX(a.Longitude);
        double y1 = LatToY(a.Latitude);
        double x2 = LonToX(b.Longitude);
        double y2 = LatToY(b.Latitude);

        double dx = x2 - x1;
        double dy = y2 - y1;

        if (dx == 0 && dy == 0)
            return EarthDistanceCalculator.CalculateHaversineDistance(p.Latitude, p.Longitude, a.Latitude, a.Longitude); // Segment ist nur ein Punkt

        // Parameter t zur Bestimmung des Fußpunkts auf der Strecke
        double t = ((x - x1) * dx + (y - y1) * dy) / (dx * dx + dy * dy);
        t = Math.Max(0, Math.Min(1, t)); // Clamp to [0, 1]

        double projX = x1 + t * dx;
        double projY = y1 + t * dy;

        double latProj = YToLat(projY);
        double lonProj = XToLon(projX);

        return EarthDistanceCalculator.CalculateHaversineDistance(p.Latitude, p.Longitude, latProj, lonProj);
    }
    private const double EarthRadius = 6371000.0;
    private double LatToY(double lat) => EarthRadius * Math.Log(Math.Tan(Math.PI / 4 + (lat * Math.PI / 180) / 2));
    private double LonToX(double lon) => EarthRadius * lon * Math.PI / 180;
    private double YToLat(double y) => (2 * Math.Atan(Math.Exp(y / EarthRadius)) - Math.PI / 2) * 180 / Math.PI;
    private double XToLon(double x) => x * 180 / (Math.PI * EarthRadius);

    private async Task DetectLaunchHeight(Aircraft aircraft, DateTime takeoffTime)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
        var flightData = await dbContext.FlightData
            .Where(x => x.AircraftId == aircraft.Id && x.Timestamp >= takeoffTime)
            .OrderBy(x => x.Timestamp)
            .ToListAsync();
        var entry = await dbContext.FlightbookEntry
            .Include(e => e.Aircraft)
            .FirstOrDefaultAsync(e => e.AircraftId == aircraft.Id && e.TakeOffTimestamp == takeoffTime);
        if (entry == null)
        {
            return;
        }

        var launchHeightPeak = DetectLaunchHeightFromPeak(flightData, (LaunchType)entry.LaunchType);
        var launchHeightThermal = DetectLaunchHeightFromThermalTurning(flightData);
        var launchHeightThermalImproved = DetectLaunchHeightFromThermalTurningImproved(flightData);

        if (launchHeightPeak.HasValue)
        {
            _logger.LogInformation($"[Flightbook] Starthöhe ({launchHeightPeak}m) von {aircraft.CallSign} erkannt durch \"Gipfelerkennung\"");
        }
        if (launchHeightThermal.HasValue)
        {
            _logger.LogInformation($"[Flightbook] Starthöhe ({launchHeightThermal}m) von {aircraft.CallSign} erkannt durch \"Thermikkurbeln\"");
        }
        if (launchHeightThermalImproved.HasValue)
        {
            _logger.LogInformation($"[Flightbook] Starthöhe ({launchHeightThermalImproved}m) von {aircraft.CallSign} erkannt durch \"Thermikkurbeln (verbessert)\"");
        }

        if (!launchHeightPeak.HasValue)
        {
            _pendingLaunchHeightQueue.Enqueue((aircraft, takeoffTime));
            return;
        }
        int? launchHeight = launchHeightPeak ?? launchHeightThermal ?? launchHeightThermalImproved;
        launchHeight = launchHeight.HasValue ? (int?)Math.Round((launchHeight.Value - GroundHeight) / 10.0) * 10 : null;

        entry.LaunchHeight = launchHeight;
        await dbContext.SaveChangesAsync();
    }

    private int? DetectLaunchHeightFromPeak(List<FlightPathItem> data, LaunchType launchType)
    {
        var peakWindowSize = launchType == LaunchType.Winch ? LaunchHeightPeakWindowSizeWinch : LaunchHeightPeakWindowSizeTow; 
        for (int i = peakWindowSize; i < data.Count - peakWindowSize; i++)
        {
            var current = data[i];
            var before = data.GetRange(i - peakWindowSize, peakWindowSize);
            var after = data.GetRange(i + 1, peakWindowSize);

            if (before.All(p => p.Altitude < current.Altitude) && after.All(p => p.Altitude < current.Altitude))
            {
                return current.Altitude;
            }
        }
        return null;
    }

    private int? DetectLaunchHeightFromThermalTurning(List<FlightPathItem> data)
    {
        for (int i = 0; i < data.Count - LaunchHeightCourseChangeWindow; i++)
        {
            double totalChange = 0;
            for (int j = 0; j < LaunchHeightCourseChangeWindow - 1; j++)
            {
                var a = data[i + j];
                var b = data[i + j + 1];
                double delta = Math.Abs(b.Course - a.Course);
                if (delta > 180) delta = 360 - delta;
                totalChange += delta;
            }

            if (totalChange > LaunchHeightCourseChangeThreshold * LaunchHeightCourseChangeWindow)
            {
                return data[i].Altitude;
            }
        }
        return null;
    }

    private int? DetectLaunchHeightFromThermalTurningImproved(List<FlightPathItem> data)
    {
        if (data.Count < 2) return null;

        int startIndex = -1;
        double totalChange = 0;
        double totalTime = 0;

        for (int i = 1; i < data.Count; i++)
        {
            var prev = data[i - 1];
            var curr = data[i];

            var timeDiff = (curr.Timestamp - prev.Timestamp).TotalSeconds;
            if (timeDiff < 1 || timeDiff > 20) continue; // Ignore unreliable or gappy data

            double delta = Math.Abs(curr.Course - prev.Course);
            if (delta > 180) delta = 360 - delta;

            double changeRate = delta / timeDiff;

            if (changeRate >= LaunchHeightTurningThresholdPerSecond)
            {
                if (startIndex == -1)
                    startIndex = i - 1;

                totalChange += delta;
                totalTime += timeDiff;

                if (totalTime >= LaunchHeightTurningMinDuration)
                {
                    return data[startIndex].Altitude;
                }
            }
            else
            {
                // Reset if pattern breaks
                startIndex = -1;
                totalChange = 0;
                totalTime = 0;
            }
        }

        return null;
    }
}
