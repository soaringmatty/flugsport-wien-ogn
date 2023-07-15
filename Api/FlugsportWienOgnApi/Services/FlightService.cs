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

    public async Task<IEnumerable<Flight>> GetFlights([FromQuery] double? maxLat, [FromQuery] double? minLat, [FromQuery] double? maxLng, [FromQuery] double? minLng)
    {
        var timeSinceLastRequest = DateTime.Now - _lastUpdateTime;
        if (timeSinceLastRequest.TotalMilliseconds < 2900)
        {
            _logger.LogTrace($"Time since last request: {timeSinceLastRequest.TotalMilliseconds} ms. Returning cached flights...");
            return Flights;
        }

        string? urlParams;
        if (maxLat == null || minLat == null || maxLng == null || minLng == null)
        {
            urlParams = "?showOnlyGliders=true&a=49&b=17.2&c=46.6&d=9.4";
        }
        else
        {
            urlParams = $"?showOnlyGliders=true&a={maxLat}&b={maxLng}&c={minLat}&d={minLng}";
        }
        string url = $"https://api.glideandseek.com/v2/aircraft{urlParams}";
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetFromJsonAsync<GetOgnFlightsResponse>(url);
        if (response != null && response.Success)
        {
            _lastUpdateTime = DateTime.Now;
            Flights = Mapping.MapOgnFlightsResponseToFlights(response.Message);
            return Flights;
        }
        return null;
    }
}
