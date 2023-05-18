using Microsoft.AspNetCore.Mvc;
using FlugsportWienOgnApi.Models.GlideAndSeek;
using FlugsportWienOgnApi.Models.Core;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FlugsportWienOgnApi.Constants;

namespace FlugsportWienOgnApi.Controllers
{
    [ApiController]
    [Route("flights")]
    public class FlightController : ControllerBase
    {
        private readonly ILogger<FlightController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _flightBookUrl = "https://flightbook.glidernet.org/api/logbook/LOXN/";
        // https://api.glideandseek.com/v2/history/320533

        public FlightController(ILogger<FlightController> logger, HttpClient httpClient, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights([FromQuery] double? minLat, [FromQuery] double? maxLat, [FromQuery] double? minLng, [FromQuery] double? maxLng, [FromQuery] bool showAllGliders = false, [FromQuery] bool showPrivateGliders = false)
        {
            string url = "https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a=52&b=22&c=43&d=7";
            //string url = "https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a=49&b=17.20&c=46.60&d=9.40";
            //string url = $"https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a={maxLat}&b={maxLng}&c={minLat}&d={minLng}";
            var response = await _httpClient.GetFromJsonAsync<GetOgnFlightsResponse>(url);
            if (response != null && response.Success)
            {
                var flights = MapOgnFlightsResponseToFlights(response.Message);
                return Ok(flights);
            }
            return BadRequest();
        }

        [HttpGet("gliders")]
        public async Task<ActionResult<IEnumerable<GliderListItem>>> GetGliderList()
        {
            List<GliderListItem> gliderList = new List<GliderListItem>();

            List<Flight> flights = new List<Flight>();
            string url = "https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a=52&b=22&c=43&d=7";
            var response = await _httpClient.GetFromJsonAsync<GetOgnFlightsResponse>(url);
            if (response != null && response.Success)
            {
                flights = MapOgnFlightsResponseToFlights(response.Message).ToList();
            }
            foreach (var glider in KnownGliders.ClubGliders)
            {
                var flight = flights.FirstOrDefault(x => x.FlarmId == glider.FlarmId);
                if (flight == null)
                {
                    gliderList.Add(new GliderListItem
                    {
                        Owner = glider.Owner,
                        Registration = glider.Registration,
                        RegistrationShort = glider.RegistrationShort,
                        Model = glider.Model,
                        TakeOffTimestamp = -1,
                        Status = glider.Model.Contains("500") ? GliderStatus.Flying : GliderStatus.NoSignal,
                        Pilot = "Not implemented",
                        DistanceFromHome = -1,
                        Altitude = -1,
                    });
                }
                else
                {
                    var gliderStatus = (flight.Speed > 10 && flight.HeightAGL > 10) ? GliderStatus.Flying : GliderStatus.OnGround;
                    gliderList.Add(new GliderListItem
                    {
                        Owner = glider.Owner,
                        Registration = glider.Registration,
                        RegistrationShort = glider.RegistrationShort,
                        Model = glider.Model,
                        TakeOffTimestamp = -1,
                        Status = gliderStatus,
                        Pilot = "Not implemented",
                        DistanceFromHome = -1,
                        Altitude = (int)flight.HeightMSL,
                    });
                }
            }
            gliderList.Sort();
            return Ok(gliderList);
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

        [HttpGet("{flarmId}/history")]
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