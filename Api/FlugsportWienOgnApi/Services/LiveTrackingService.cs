using Aprs.Models;
using Aprs;
using FlugsportWienOgn.Database;
using FlugsportWienOgn.Database.Entities;
using FlugsportWienOgnApi.Utils;
using FlugsportWienOgnApi.Models.Core;
using Microsoft.EntityFrameworkCore;
using FlugsportWienOgnApi.Models.LiveTracking;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace FlugsportWienOgnApi.Services;

public class LiveTrackingService
{
    private readonly LiveGliderService _liveGliderService;
    private readonly AustriaGeoCalculator _austriaGeoCalculator;
    private readonly AircraftProvider _aircraftProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<OgnConfig> _config;
    private readonly ILogger<LiveTrackingService> _logger;

    private readonly ConcurrentDictionary<string, FlightDataBuffer> _buffers = new();
    private readonly TimeSpan _aggregationWindow = TimeSpan.FromSeconds(3);
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));

    public LiveTrackingService(ILogger<LiveTrackingService> logger, LiveGliderService liveGliderService, IServiceProvider serviceProvider, AircraftProvider aircraftProvider, IOptions<OgnConfig> config)
    {
        _config = config;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _aircraftProvider = aircraftProvider;
        _austriaGeoCalculator = new AustriaGeoCalculator();
        _liveGliderService = liveGliderService;
        _liveGliderService.FlightDataReceived += BufferFlightData;
    }

    private void BufferFlightData(FlightData flightData)
    {
        var buffer = _buffers.GetOrAdd(flightData.FlarmId, _ => new FlightDataBuffer());
        lock (buffer)
        {
            buffer.Buffer.Add(flightData);
        }
    }

    public async Task StartFlushBufferLoop(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var (flarmId, buffer) in _buffers)
            {
                List<FlightData> snapshot;
                lock (buffer)
                {
                    if (!buffer.Buffer.Any()) continue;

                    var windowStart = DateTime.UtcNow - _aggregationWindow;
                    if (buffer.Buffer.Max(f => f.ReceiverTimeStamp) - buffer.LastFlushed < _aggregationWindow)
                        continue;

                    snapshot = buffer.Buffer.ToList();
                    buffer.Buffer.Clear();
                    buffer.LastFlushed = DateTime.UtcNow;
                }

                var aggregated = AggregateBuffer(snapshot);
                await AddFlightDataToDatabaseAsync(aggregated, stoppingToken);
            }
        }
    }

    private FlightData AggregateBuffer(List<FlightData> buffer)
    {
        return new FlightData(
            buffer.First().FlarmId,
            buffer.Average(b => b.Speed),
            buffer.Average(b => b.Altitude),
            buffer.Average(b => b.VerticalSpeed),
            buffer.Average(b => b.TurnRate),
            buffer.Average(b => b.Course),
            buffer.Average(b => b.Latitude),
            buffer.Average(b => b.Longitude),
            buffer.Max(b => b.ReceiverTimeStamp)
        );
    }

    private async Task AddFlightDataToDatabaseAsync(FlightData flightData, CancellationToken token)
    {
        if (_config.Value.AustrianAirspaceOnly)
        {
            // Filter to only flight data in austrian airspace
            var isInAustria = _austriaGeoCalculator.IsPointInAustria(flightData.Longitude, flightData.Latitude);
            if (!isInAustria)
            {
                return;
            }
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
        var aircraftId = AddOrUpdatePlaneEntity(flightData, dbContext);



        // Ignore flight data if more recent position data is already in database
        var lastTimestamp = dbContext.FlightData
            .Where(flightPathItem => flightPathItem.AircraftId == aircraftId)
            .Max(flightPathItem => (DateTime?)flightPathItem.Timestamp);
        if (lastTimestamp.HasValue && flightData.ReceiverTimeStamp <= lastTimestamp.Value)
        {
            return;
        }

        var flightPathItem = new FlightPathItem
        {
            AircraftId = aircraftId,
            Latitude = (float)Math.Round(flightData.Latitude, 5),
            Longitude = (float)Math.Round(flightData.Longitude, 5),
            Speed = (int)Math.Round(flightData.Speed),
            Altitude = (int)Math.Round(flightData.Altitude),
            VerticalSpeed = flightData.VerticalSpeed,
            Timestamp = flightData.ReceiverTimeStamp,
            Receiver = flightData.Receiver,
        };

        dbContext.FlightData.Add(flightPathItem);
        await dbContext.SaveChangesAsync(token);
    }

    private int AddOrUpdatePlaneEntity(FlightData flightData, FlightDbContext dbContext)
    {
        // Check if the plane already exists in the database based on a unique identifier, e.g., PlaneId or CallSign
        var existingPlane = dbContext.Aircraft
            .FirstOrDefault(p => p.FlarmId == flightData.FlarmId);

        if (existingPlane != null)
        {
            // Plane exists, so update the current flight data
            existingPlane.Latitude = flightData.Latitude;
            existingPlane.Longitude = flightData.Longitude;
            existingPlane.Speed = (int)Math.Round(flightData.Speed);
            existingPlane.Altitude = (int)Math.Round(flightData.Altitude);
            existingPlane.VerticalSpeed = flightData.VerticalSpeed;
            existingPlane.LastUpdate = flightData.ReceiverTimeStamp;
            dbContext.Aircraft.Update(existingPlane);
            return existingPlane.Id;
        }
        else
        {
            // Plane doesn't exist, add a new plane entry
            var aircraftData = _aircraftProvider.Load(flightData.FlarmId);
            var newPlane = new FlugsportWienOgn.Database.Entities.Aircraft
            {
                FlarmId = flightData.FlarmId,
                Registration = flightData.FlarmId,
                CallSign = $"? {flightData.FlarmId.Substring(flightData.FlarmId.Length - 2)}",
                Model = "Unbekannt",
                Latitude = flightData.Latitude,
                Longitude = flightData.Longitude,
                Speed = (int)Math.Round(flightData.Speed),
                Altitude = (int)Math.Round(flightData.Altitude),
                VerticalSpeed = flightData.VerticalSpeed,
                LastUpdate = flightData.ReceiverTimeStamp,
                AircraftType = (int)AircraftType.Unknown
            };
            // Add aircraft data if it is registered
            if (aircraftData != null)
            {
                var calculatedCallSign =
                    (!string.IsNullOrWhiteSpace(aircraftData.Registration) && aircraftData.Registration.Length >= 4) ?
                    aircraftData.Registration?.Substring(aircraftData.Registration.Length - 2) :
                    "??";
                newPlane.Registration = aircraftData.Registration;
                newPlane.CallSign = !string.IsNullOrEmpty(aircraftData.CallSign) ? aircraftData.CallSign : calculatedCallSign;
                newPlane.Model = aircraftData.Model;
                newPlane.AircraftType = (int)MapGlidernetAircraftType(aircraftData.AircraftType);
            }
            dbContext.Aircraft.Add(newPlane);
            dbContext.SaveChanges();
            return newPlane.Id;
        }
    }

    private AircraftType MapGlidernetAircraftType(GlidernetAircraftType rawType)
    {
        switch (rawType)
        {
            case GlidernetAircraftType.Glider:
                return AircraftType.Glider;
            case GlidernetAircraftType.Motorplane:
            case GlidernetAircraftType.Ultralight:
                return AircraftType.Motorplane;
            case GlidernetAircraftType.Helicopter:
                return AircraftType.Helicopter;
            case GlidernetAircraftType.HangOrParaglider:
                return AircraftType.HangOrParaglider;
            default:
                return AircraftType.Unknown;
        }
    }
}

public class FlightDataBuffer
{
    public List<FlightData> Buffer { get; set; } = new();
    public DateTime LastFlushed { get; set; } = DateTime.MinValue;
}
