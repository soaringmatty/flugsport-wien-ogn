using Microsoft.AspNetCore.Mvc;
using FlugsportWienOgnApi.Models.GlideAndSeek;
using FlugsportWienOgnApi.Models.Core;

namespace FlugsportWienOgnApi.Controllers
{
    [ApiController]
    [Route("flights")]
    public class FlightController : ControllerBase
    {
        private readonly ILogger<FlightController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _flightBookUrl = "https://flightbook.glidernet.org/api/logbook/LOXN/";

        public FlightController(ILogger<FlightController> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights([FromQuery] bool showAllGliders = false, [FromQuery] bool showPrivateGliders = false)
        {
            string url = "https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a=48.72&b=46.65&c=16.70&d=11.00";
            var response = await _httpClient.GetFromJsonAsync<GetOgnFlightsResponse>(url);
            if (response != null && response.Success)
            {
                var flights = MapOgnFlightsResponseToFlights(response.Message);
                return Ok(flights);
            }
            return BadRequest();
        }

        [HttpGet("{flarmId}/path")]
        public async Task<ActionResult<string>> GetFlightPath(string flarmId)
        {
            string url = $"https://api.glideandseek.com/v2/track/{flarmId}";
            var response = await _httpClient.GetFromJsonAsync<GetOgnFlightPathResponse>(url);
            if (response != null && response.Success)
            {;
                return Ok(response.Message);
            }
            return BadRequest();
        }

        private IEnumerable<Flight> MapOgnFlightsResponseToFlights(IEnumerable<GetOgnFlightsResponseDto> rawFlights)
        {
            List<Flight> flights = new();
            foreach (var rawFlight in rawFlights)
            {
                flights.Add(new Flight
                {
                    FlarmId = rawFlight.FlarmID,
                    DisplayName = rawFlight.DisplayName,
                    Registration = rawFlight.Registration,
                    Type = rawFlight.Type,
                    Model = rawFlight.Model,
                    Latitude = rawFlight.Lat,
                    Longitude = rawFlight.Lng,
                    HeightMSL = rawFlight.Altitude,
                    HeightAGL = rawFlight.Agl,
                    Timestamp = rawFlight.Timestamp,
                    Speed = rawFlight.Speed,
                    Vario = rawFlight.Vario,
                    VarioAverage = rawFlight.VarioAverage,
                    Receiver = rawFlight.Receiver,
                    ReceiverPosition = rawFlight.ReceiverPosition
                });
            }

            return flights;
        }
    }
}