using Arps.Models;
using Arps;
using FlugsportWienOgn.Database;
using FlugsportWienOgn.Database.Entities;
using FlugsportWienOgnApi.Utils;
using FlugsportWienOgnApi.Models.Core;
using Microsoft.EntityFrameworkCore;
using FlugsportWienOgnApi.Models.LiveTracking;
using FlugsportWienOgnApi.Models.Aprs;
using FlugsportWienOgnApi.Models.GlideAndSeek;
//using FlightPathItem = FlugsportWienOgnApi.Models.LiveTracking.FlightPathItem;

namespace FlugsportWienOgnApi.Services;

public class LiveTrackingService
{
    private readonly LiveGliderService _liveGliderService;
    private readonly AustriaGeoCalculator _austriaGeoCalculator;
    private readonly AircraftProvider _aircraftProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LiveTrackingService> _logger;

    public LiveTrackingService(ILogger<LiveTrackingService> logger, LiveGliderService liveGliderService, IServiceProvider serviceProvider, AircraftProvider aircraftProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _aircraftProvider = aircraftProvider;
        _austriaGeoCalculator = new AustriaGeoCalculator();
        _liveGliderService = liveGliderService;
        _liveGliderService.OnDataReceived += AddFlightDataToDatabase;
        _logger.LogInformation("Airplane Tracking has started");
    }

    public async Task<IEnumerable<Flight>> GetFlights(string? selectedFlarmId, bool? glidersOnly, bool? clubGlidersOnly, double? maxLat, double? minLat, double? maxLng, double? minLng)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

            var currentFlights = await dbContext.Planes
                .Select(plane => new Flight
                {
                    FlarmId = plane.FlarmId,
                    DisplayName = plane.CallSign,
                    Registration = plane.Registration,
                    Type = GliderType.Foreign,
                    AircraftType = (AircraftType)plane.AircraftType,
                    Model = plane.Model,
                    Latitude = plane.Latitude,
                    Longitude = plane.Longitude,
                    HeightMSL = plane.Altitude,
                    HeightAGL = -1,
                    Timestamp = new DateTimeOffset(plane.LastUpdate).ToUnixTimeMilliseconds(), // Convert timestamp
                    Speed = plane.Speed,
                    Vario = plane.VerticalSpeed,
                    VarioAverage = -1
                })
                .ToListAsync();
            return currentFlights;
        }
    }

    public async Task<IEnumerable<double[]>> GetFlightPath(string flarmId)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

            // Find the plane by FlarmId
            var plane = await dbContext.Planes
                .FirstOrDefaultAsync(p => p.FlarmId == flarmId);

            if (plane == null)
            {
                // Handle the case where the plane is not found (return empty list or throw exception)
                return Enumerable.Empty<double[]>();
            }

            // Get the flight path items for the plane using PlaneId
            var flightPathItems = await dbContext.FlightData
                .Where(fd => fd.PlaneId == plane.Id) // Filter by PlaneId
                .OrderBy(fd => fd.Timestamp) // Ensure the flight path is ordered by time
                .Select(fd => new double[]
                {
                    new DateTimeOffset(fd.Timestamp).ToUnixTimeMilliseconds(), // Index 0: Timestamp
                    0,                                     // Index 1: Always 0
                    fd.Latitude,                           // Index 2: Latitude
                    fd.Longitude,                          // Index 3: Longitude
                    fd.Altitude,                           // Index 4: Altitude
                    286                                    // Index 5: Ground Height (static value)
                })
                .ToListAsync(); // Execute query and retrieve the flight path

            return flightPathItems;
        }
    }

    public async Task<IEnumerable<FlightPathItemDto>> GetFlightPathAsObjects(string flarmId)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

            // Find the plane by FlarmId
            var plane = await dbContext.Planes
                .FirstOrDefaultAsync(p => p.FlarmId == flarmId);

            if (plane == null)
            {
                // Handle the case where the plane is not found (return empty list or throw exception)
                return Enumerable.Empty<FlightPathItemDto>();
            }

            // Get the flight path items for the plane using PlaneId
            var flightPathItems = await dbContext.FlightData
                .Where(fd => fd.PlaneId == plane.Id) // Filter by PlaneId
                .OrderBy(fd => fd.Timestamp)
                // Ensure the flight path is ordered by time
                .Select(fd => new FlightPathItemDto
                {
                    Location = new Coordinate(fd.Latitude, fd.Longitude),
                    Altitude = fd.Altitude,
                    Speed = fd.Speed,
                    VerticalSpeed = fd.VerticalSpeed,
                    Timestamp = fd.Timestamp,
                    Receiver = fd.Receiver
                })
                .ToListAsync(); // Execute query and retrieve the flight path

            return flightPathItems;
        }
    }


    private void AddFlightDataToDatabase(FlightData flightData)
    {
        var isInAustria = _austriaGeoCalculator.IsPointInAustria(flightData.Longitude, flightData.Latitude);
        if (!isInAustria) { return; }

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
            var planeId = this.AddOrUpdatePlaneEntity(flightData, dbContext);

            var flightPathItem = new FlugsportWienOgn.Database.Entities.FlightPathItem
            {
                PlaneId = planeId,
                Latitude = flightData.Latitude,
                Longitude = flightData.Longitude,
                Speed = (int)Math.Round(flightData.Speed),
                Altitude = (int)Math.Round(flightData.Altitude),
                VerticalSpeed = flightData.VerticalSpeed,
                Timestamp = DateTime.UtcNow,
                Receiver = flightData.Receiver,
            };

            dbContext.FlightData.Add(flightPathItem);
            dbContext.SaveChanges();
        }
    }

    private int AddOrUpdatePlaneEntity(FlightData flightData, FlightDbContext dbContext)
    {
        // Check if the plane already exists in the database based on a unique identifier, e.g., PlaneId or CallSign
        var existingPlane = dbContext.Planes
            .FirstOrDefault(p => p.FlarmId == flightData.FlarmId);

        if (existingPlane != null)
        {
            // Plane exists, so update the current flight data
            existingPlane.Latitude = flightData.Latitude;
            existingPlane.Longitude = flightData.Longitude;
            existingPlane.Speed = (int)Math.Round(flightData.Speed);
            existingPlane.Altitude = (int)Math.Round(flightData.Altitude);
            existingPlane.VerticalSpeed = flightData.VerticalSpeed;
            existingPlane.LastUpdate = flightData.DateTime;
            dbContext.Planes.Update(existingPlane);
            return existingPlane.Id;
        }
        else
        {
            // Plane doesn't exist, add a new plane entry
            var aircraftData = _aircraftProvider.Load(flightData.FlarmId);
            var newPlane = new Plane
            {
                FlarmId = flightData.FlarmId,
                Registration = flightData.FlarmId,
                CallSign = $"? {flightData.FlarmId.Substring(flightData.FlarmId.Length - 2)}",
                Model = "Unknown",
                Latitude = flightData.Latitude,
                Longitude = flightData.Longitude,
                Speed = (int)Math.Round(flightData.Speed),
                Altitude = (int)Math.Round(flightData.Altitude),
                VerticalSpeed = flightData.VerticalSpeed,
                LastUpdate = flightData.DateTime,
                AircraftType = (int)AircraftType.Unknown
            };
            // Add aircraft data if it is registered
            if (aircraftData != null)
            {
                var calculatedCallSign = aircraftData.Registration?.Substring(aircraftData.Registration.Length - 2);
                newPlane.Registration = aircraftData.Registration;
                newPlane.CallSign = !string.IsNullOrEmpty(aircraftData.CallSign) ? aircraftData.CallSign : calculatedCallSign;
                newPlane.Model = aircraftData.Model;
                newPlane.AircraftType = (int)MapGlidernetAircraftType(aircraftData.AircraftType);
            }
            dbContext.Planes.Add(newPlane);
            dbContext.SaveChanges();
            return newPlane.Id;
        }
    }

    private Plane AddRegistrationDataToPlaneEntity(Plane existingPlane)
    {
        var aircraftData = _aircraftProvider.Load(existingPlane.FlarmId);
        if (aircraftData == null) { return existingPlane; }

        return existingPlane;
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
