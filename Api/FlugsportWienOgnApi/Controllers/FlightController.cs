using Microsoft.AspNetCore.Mvc;
using FlugsportWienOgnApi.Models.Core;
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights([FromQuery] string? selectedFlarmId, [FromQuery] bool? glidersOnly, [FromQuery] bool? clubGlidersOnly, [FromQuery] double? maxLat, [FromQuery] double? minLat, [FromQuery] double? maxLng, [FromQuery] double? minLng, [FromQuery] int? lastUpdateMaxMinutes)
        {
            var flights = await _flightService.GetFlights(selectedFlarmId, glidersOnly, clubGlidersOnly, maxLat, minLat, maxLng, minLng, lastUpdateMaxMinutes);
            if (flights != null)
            {
                return Ok(flights);
            }
            return BadRequest();
        }

        [HttpGet("find/{searchText}")]
        public async Task<ActionResult<IEnumerable<AircraftSearchResultItem>>> Search(string searchText, [FromQuery] int? take)
        {
            var result = await _flightService.SearchAircraftAsync(searchText, take);
            return Ok(result);
        }

        [HttpGet("{flarmId}/history")]
        public async Task<ActionResult<IEnumerable<object[]>>> GetFlightPathFromDatabase(string flarmId, [FromQuery] DateTimeOffset? startTimestamp, [FromQuery] DateTimeOffset? endTimestamp)
        {
            var flightPath = await _flightService.GetFlightPath(flarmId, startTimestamp, endTimestamp);
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
    }
}