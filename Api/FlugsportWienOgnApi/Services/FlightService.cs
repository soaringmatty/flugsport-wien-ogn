using FlugsportWienOgnApi.Models.Core;
using FlugsportWienOgnApi.Models.GlideAndSeek;
using FlugsportWienOgnApi.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace FlugsportWienOgnApi.Services;

public class FlightService
{
    private readonly ILogger<FlightService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private DateTime _lastUpdateTime;

    public IEnumerable<Flight> Flights { get; set; }

    public FlightService(ILogger<FlightService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        Flights = new List<Flight>();
    }

    public async Task<IEnumerable<Flight>> GetFlights(string? selectedFlarmId, bool? clubGlidersOnly, double? maxLat, double? minLat,  double? maxLng, double? minLng)
    {
        var timeSinceLastRequest = DateTime.Now - _lastUpdateTime;
        if (timeSinceLastRequest.TotalMilliseconds < 2900)
        {
            _logger.LogTrace($"Time since last request: {timeSinceLastRequest.TotalMilliseconds} ms. Returning cached flights...");
            return FilterFlights(selectedFlarmId, clubGlidersOnly, maxLat, minLat, maxLng, minLng);
        }
        string url = $"https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a=49&b=17.2&c=46.6&d=9.4";
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetFromJsonAsync<GetOgnFlightsResponse>(url);
        if (response != null && response.Success)
        {
            _lastUpdateTime = DateTime.Now;
            Flights = Mapping.MapOgnFlightsResponseToFlights(response.Message);
            return FilterFlights(selectedFlarmId, clubGlidersOnly, maxLat, minLat, maxLng, minLng);
        }
        return null;
    }

    private IEnumerable<Flight> FilterFlights(string? selectedFlarmId, bool? clubGlidersOnly, double? maxLat, double? minLat, double? maxLng, double? minLng)
    {
        var flightsToReturn = Flights.Where(
            x => x.FlarmId == selectedFlarmId ||
            (
                x.Latitude >= minLat && x.Latitude <= maxLat && x.Longitude >= minLng && x.Longitude <= maxLng &&
                AustriaGeoCalculator.IsPointInAustria(x.Longitude, x.Latitude)
            )
        );
        if (clubGlidersOnly == true)
        {
            flightsToReturn = flightsToReturn.Where(x => KnownGliders.ClubGliders.Any(glider => glider.FlarmId == x.FlarmId));
        }
        return flightsToReturn;
    }
}
