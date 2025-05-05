using FlugsportWienOgn.Database;
using FlugsportWienOgn.Database.Entities;
using FlugsportWienOgnApi.Models.Core;
using FlugsportWienOgnApi.Models.GlideAndSeek;
using FlugsportWienOgnApi.Models.LiveTracking;
using FlugsportWienOgnApi.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reactive;

namespace FlugsportWienOgnApi.Services;

public class FlightService
{
    private DateTime _lastUpdateTime;
    private readonly AustriaGeoCalculator _austriaGeoCalculator;
    private readonly KnownAircraftService _knownAircraftService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<OgnConfig> _config;
    private readonly ILogger<FlightService> _logger;

    public IEnumerable<Flight> Flights { get; set; }

    public FlightService(ILogger<FlightService> logger, IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider, IOptions<OgnConfig> config, KnownAircraftService knownAircraftService)
    {
        _logger = logger;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;
        _austriaGeoCalculator = new AustriaGeoCalculator();
        _knownAircraftService = knownAircraftService;
        Flights = new List<Flight>();
    }

    public async Task<IEnumerable<Flight>> GetGlideAndSeekFlights(string? selectedFlarmId, bool? glidersOnly, bool? clubGlidersOnly, double? maxLat, double? minLat,  double? maxLng, double? minLng)
    {
        var timeSinceLastRequest = DateTime.Now - _lastUpdateTime;
        if (timeSinceLastRequest.TotalMilliseconds < 2900)
        {
            _logger.LogTrace($"Time since last request: {timeSinceLastRequest.TotalMilliseconds} ms. Returning cached flights...");
            return FilterFlights(selectedFlarmId, glidersOnly, clubGlidersOnly, maxLat, minLat, maxLng, minLng);
        }
        string url = $"https://api.glideandseek.com/v2/aircraft?showOnlyGliders=false&a=49&b=17.2&c=46.6&d=9.4";
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetFromJsonAsync<GetOgnFlightsResponse>(url);
        if (response != null && response.Success)
        {
            _lastUpdateTime = DateTime.Now;
            Flights = Mapping.MapOgnFlightsResponseToFlights(response.Message, _knownAircraftService);
            return FilterFlights(selectedFlarmId, glidersOnly, clubGlidersOnly, maxLat, minLat, maxLng, minLng);
        }
        return null;
    }



    public async Task<IEnumerable<Flight>> GetFlights(string? selectedFlarmId, bool? glidersOnly, bool? clubGlidersOnly, double? maxLat, double? minLat, double? maxLng, double? minLng, int? lastUpdateMaxMinutes)
    {
        if (!lastUpdateMaxMinutes.HasValue) {
            lastUpdateMaxMinutes = _config.Value.FlightDataMaxAge;
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

            var flightQuery = dbContext.Aircraft.AsQueryable();

            // Filter flights within a certain latitude and longitude range (if parameters are set)
            if (minLat.HasValue || maxLat.HasValue || minLng.HasValue || maxLng.HasValue)
            {
                var minLatitude = minLat ?? double.MinValue;
                var maxLatitude = maxLat ?? double.MaxValue;
                var minLongitude = minLng ?? double.MinValue;
                var maxLongitude = maxLng ?? double.MaxValue;

                flightQuery = flightQuery.Where(x =>
                    x.Latitude >= minLatitude && x.Latitude <= maxLatitude &&
                    x.Longitude >= minLongitude && x.Longitude <= maxLongitude);
            }
            // Filter flights to FlugsportWien related planes only (if parameter is set)
            if (clubGlidersOnly == true)
            {
                flightQuery = flightQuery.Where(x => _knownAircraftService.ClubGlidersAndMotorplanes.Any(glider => glider.FlarmId == x.FlarmId));
            }
            // Filter flights to aircraft type "glider" only (if parameter is set)
            else if (glidersOnly == true)
            {
                flightQuery = flightQuery.Where(x => (AircraftType)x.AircraftType == AircraftType.Unknown);
            }
            // Filter flight according to last updated timestamp
            flightQuery = flightQuery.Where(x => x.LastUpdate > DateTime.Now.AddMinutes(lastUpdateMaxMinutes.Value * -1));
            var currentFlights = await flightQuery.Select(plane => new Flight
            {
                FlarmId = plane.FlarmId,
                DisplayName = plane.CallSign,
                Registration = plane.Registration,
                Type = _knownAircraftService.GetGliderOwnershipByFlarmId(plane.FlarmId),
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

    /// <summary>
    /// Gets full flight path of a specific aircraft as plain data array (similar to GlideAndSeek)
    /// </summary>
    /// <param name="flarmId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<double[]>> GetFlightPath(string flarmId)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

            // Find the plane by FlarmId
            var plane = await dbContext.Aircraft
                .FirstOrDefaultAsync(p => p.FlarmId == flarmId);

            if (plane == null)
            {
                // Handle the case where the plane is not found (return empty list or throw exception)
                return Enumerable.Empty<double[]>();
            }

            // Get the flight path items for the plane using PlaneId
            var flightPathItems = await dbContext.FlightData
                .Where(fd => fd.AircraftId == plane.Id) // Filter by PlaneId
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

    /// <summary>
    /// Gets full flight path of a specific aircraft as json
    /// </summary>
    /// <param name="flarmId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<FlightPathItemDto>> GetFlightPathAsObjects(string flarmId)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

            // Find the plane by FlarmId
            var plane = await dbContext.Aircraft
                .FirstOrDefaultAsync(p => p.FlarmId == flarmId);

            if (plane == null)
            {
                // Handle the case where the plane is not found (return empty list or throw exception)
                return Enumerable.Empty<FlightPathItemDto>();
            }

            // Get the flight path items for the plane using PlaneId
            var flightPathItems = await dbContext.FlightData
                .Where(fd => fd.AircraftId == plane.Id) // Filter by PlaneId
                .OrderBy(fd => fd.Timestamp)
                // Ensure the flight path is ordered by time
                .Select(fd => new FlightPathItemDto
                {
                    Location = new Coordinate(fd.Latitude, fd.Longitude),
                    Altitude = fd.Altitude,
                    Speed = fd.Speed,
                    VerticalSpeed = fd.VerticalSpeed,
                    Timestamp = fd.Timestamp,
                    UnixTimestamp = new DateTimeOffset(fd.Timestamp).ToUnixTimeMilliseconds(),
                })
                .ToListAsync(); // Execute query and retrieve the flight path

            return flightPathItems;
        }
    }

    /// <summary>
    /// Searches for aircraft in the database by a given search text
    /// </summary>
    public async Task<List<AircraftSearchResultItem>> SearchAircraftAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return new List<AircraftSearchResultItem>();

        var term = searchText.Trim().ToLowerInvariant();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
        var known = scope.ServiceProvider.GetRequiredService<KnownAircraftService>();

        // Find all search results
        var candidates = await db.Aircraft
            .AsNoTracking()
            .Where(a =>
                   EF.Functions.Like(a.CallSign.ToLower(), $"%{term}%") ||
                   EF.Functions.Like(a.Registration.ToLower(), $"%{term}%") ||
                   EF.Functions.Like(a.FlarmId.ToLower(), $"%{term}%"))
            .ToListAsync();

        // Prepare known glider sets for priority
        var clubSet = new HashSet<string>(
            known.ClubGlidersAndMotorplanes.Select(k => k.FlarmId),
            StringComparer.OrdinalIgnoreCase);

        var privateSet = new HashSet<string>(
            known.PrivateGliders.Select(k => k.FlarmId),
            StringComparer.OrdinalIgnoreCase);

        // Sort by priority and remove dublicates
        var ordered = candidates
            .Select(aircraft => new
            {
                Aircraft = aircraft,
                Priority = clubSet.Contains(aircraft.FlarmId) ? 1 :
                            privateSet.Contains(aircraft.FlarmId) ? 2 : 3,
                MatchRank = MatchRank(aircraft)
            })
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.MatchRank)
            .ThenBy(x => x.Aircraft.Registration ?? x.Aircraft.FlarmId)
            .DistinctBy(a => a.Aircraft.Id)
            .ToList();

        var result = ordered.Select(x => new AircraftSearchResultItem
        {
            FlarmId = x.Aircraft.FlarmId,
            RegistrationShort = x.Aircraft.CallSign,
            Registration = x.Aircraft.Registration,
            Type = _knownAircraftService.GetGliderOwnershipByFlarmId(x.Aircraft.FlarmId),
            AircraftType = (AircraftType)x.Aircraft.AircraftType,
            Model = x.Aircraft.Model,
            Latitude = x.Aircraft.Latitude,
            Longitude = x.Aircraft.Longitude,
            Timestamp = new DateTimeOffset(x.Aircraft.LastUpdate).ToUnixTimeMilliseconds(),
            Priority = x.Priority,
            MatchRank = x.MatchRank
        }).ToList();

        return result;

        int MatchRank(Aircraft aircraft)
        {
            if (!string.IsNullOrEmpty(aircraft.CallSign) && aircraft.CallSign.ToLower().Contains(term)) 
                return 1;
            if (!string.IsNullOrEmpty(aircraft.Registration) && aircraft.Registration.ToLower().Contains(term)) 
                return 2;
            if (!string.IsNullOrEmpty(aircraft.FlarmId) && aircraft.FlarmId.ToLower().Contains(term)) 
                return 3;
            return 4;
        }
    }

    private IEnumerable<Flight> FilterFlights(string? selectedFlarmId, bool? glidersOnly, bool? clubGlidersOnly, double? maxLat, double? minLat, double? maxLng, double? minLng)
    {
        var flightsToReturn = Flights.Where(
            x => x.FlarmId == selectedFlarmId ||
            (
                x.Latitude >= minLat && x.Latitude <= maxLat && x.Longitude >= minLng && x.Longitude <= maxLng &&
                _austriaGeoCalculator.IsPointInAustria(x.Longitude, x.Latitude) &&
                x.AircraftType != AircraftType.Unknown
            )
        );
        if (clubGlidersOnly == true)
        {
            flightsToReturn = flightsToReturn.Where(x => _knownAircraftService.ClubGlidersAndMotorplanes.Any(glider => glider.FlarmId == x.FlarmId));
        }
        else if (glidersOnly == true)
        {
            flightsToReturn = flightsToReturn.Where(x => x.AircraftType == AircraftType.Glider);
        }
        return flightsToReturn;
    }
}
