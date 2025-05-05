using Microsoft.AspNetCore.Mvc;
using FlugsportWienOgnApi.Models.GlideAndSeek;
using FlugsportWienOgnApi.Models.Core;
using Newtonsoft.Json.Linq;
using FlugsportWienOgnApi.Utils;
using FlugsportWienOgnApi.Services;
using FlugsportWienOgnApi.Models.LiveTracking;

namespace FlugsportWienOgnApi.Controllers
{
    [Route("flights")]
    [ApiController]
    public class FlightController : ControllerBase
    {
        private readonly ILogger<FlightController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FlightService _flightService;
        private readonly LiveTrackingService _liveTrackingService;

        public FlightController(ILogger<FlightController> logger, HttpClient httpClient, IHttpClientFactory httpClientFactory, FlightService flightService, LiveTrackingService liveTrackingService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
            _flightService = flightService;
            _liveTrackingService = liveTrackingService;
        }

        [HttpGet("glideAndSeek")]
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights([FromQuery] string? selectedFlarmId, [FromQuery] bool? glidersOnly, [FromQuery] bool? clubGlidersOnly, [FromQuery] double? maxLat, [FromQuery] double? minLat, [FromQuery] double? maxLng, [FromQuery] double? minLng)
        {
            var flights = await _flightService.GetGlideAndSeekFlights(selectedFlarmId, glidersOnly, clubGlidersOnly, maxLat, minLat, maxLng, minLng);
            if (flights != null)
            {
                return Ok(flights);
            }
            return BadRequest();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlightsFromDatabase([FromQuery] string? selectedFlarmId, [FromQuery] bool? glidersOnly, [FromQuery] bool? clubGlidersOnly, [FromQuery] double? maxLat, [FromQuery] double? minLat, [FromQuery] double? maxLng, [FromQuery] double? minLng, [FromQuery] int? lastUpdateMaxMinutes)
        {
            var flights = await _flightService.GetFlights(selectedFlarmId, glidersOnly, clubGlidersOnly, maxLat, minLat, maxLng, minLng, lastUpdateMaxMinutes);
            if (flights != null)
            {
                return Ok(flights);
            }
            return BadRequest();
        }

        [HttpGet("find")]
        public async Task<ActionResult<IEnumerable<AircraftSearchResultItem>>> Search([FromQuery] string searchText)
        {
            var result = await _flightService.SearchAircraftAsync(searchText);
            return Ok(result);
        }

        [HttpGet("{flarmId}/history")]
        public async Task<ActionResult<string>> GetFlightPathFromDatabase(string flarmId)
        {
            var flightPath = await _flightService.GetFlightPath(flarmId);
            if (flightPath != null)
            {
                return Ok(flightPath);
            }
            return BadRequest();
        }

        [HttpGet("{flarmId}/history/json")]
        public async Task<ActionResult<IEnumerable<FlightPathItemDto>>> GetFlightPathFromDatabaseAsJson(string flarmId)
        {
            var flightPath = await _flightService.GetFlightPathAsObjects(flarmId);
            if (flightPath != null)
            {
                return Ok(flightPath);
            }
            return BadRequest();
        }

        [HttpGet("{flarmId}/history/glideAndSeek")]
        public async Task<ActionResult<string>> GetFlightHistoryRaw(string flarmId)
        {
            string url = $"https://api.glideandseek.com/v2/history/{flarmId}";
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("External API request failed.");
            }

            var content = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(content);

            var message = data["message"].ToString();
            return Ok(message);
        }
    }
}