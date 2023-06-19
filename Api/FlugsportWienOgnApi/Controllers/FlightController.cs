using Microsoft.AspNetCore.Mvc;
using FlugsportWienOgnApi.Models.GlideAndSeek;
using FlugsportWienOgnApi.Models.Core;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FlugsportWienOgnApi.Utils;
using FlugsportWienOgnApi.Models.Flightbook;
using System.Runtime.CompilerServices;

namespace FlugsportWienOgnApi.Controllers
{
    [ApiController]
    [Route("/")]
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

        [HttpGet("flights")]
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights(
            [FromQuery] double? minLat = 46.6, 
            [FromQuery] double? maxLat = 49, 
            [FromQuery] double? minLng = 9.4, 
            [FromQuery] double? maxLng = 17.2, 
            [FromQuery] bool showAllGliders = false, 
            [FromQuery] bool showPrivateGliders = false)
        {
            //string url = "https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a=52&b=22&c=43&d=7";
            string url = $"https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a={maxLat}&b={maxLng}&c={minLat}&d={minLng}";
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
            var getFlightsResponse = await _httpClient.GetFromJsonAsync<GetOgnFlightsResponse>(url);
            if (getFlightsResponse != null && getFlightsResponse.Success)
            {
                flights = MapOgnFlightsResponseToFlights(getFlightsResponse.Message).ToList();
            }

            var getFlightbookResponse = await _httpClient.GetFromJsonAsync<GetFlightbookResponse>(_flightBookUrl);
            if (getFlightbookResponse == null)
            {
                return BadRequest();
            }
            var joinedFlightbook = getFlightbookResponse.flights.Join(
                getFlightbookResponse.devices.Select((device, index) => new { Device = device, Index = index }),
                flight => flight.device,
                device => device.Index,
                (flight, device) => new GetFlightbookJoinResult
                {
                    FlarmId = device.Device.address,
                    TakeOffTimestamp = flight.start_tsp,
                    LandingTimestamp = flight.stop_tsp
                });
            var knowGliderFlightbook = joinedFlightbook.Where(entry => KnownGliders.ClubGliders.Exists(glider => glider.FlarmId == entry.FlarmId));
            var latestFlightsFlightbook = knowGliderFlightbook
                .Where(jd => jd.TakeOffTimestamp.HasValue && jd.LandingTimestamp == null)
                .GroupBy(jd => jd.FlarmId)
                .Select(g => g.OrderByDescending(jd => jd.TakeOffTimestamp).FirstOrDefault());

            var homeLatitude = 47.84028;
            var homeLongitude = 16.22139;
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
                        Status = GliderStatus.NoSignal,
                        Pilot = "Not implemented",
                        DistanceFromHome = -1,
                        Altitude = -1,
                        FlarmId = glider.FlarmId,
                        Timestamp = -1,
                    });
                }
                else
                {
                    var gliderStatus = (flight.Speed > 10 && flight.HeightAGL > 10) ? GliderStatus.Flying : GliderStatus.OnGround;
                    var distanceFromHome = (int)EarthDistanceCalculator.CalculateHaversineDistance(homeLatitude, homeLongitude, flight.Latitude, flight.Longitude);
                    var flightbookEntry = latestFlightsFlightbook.FirstOrDefault(x => x.FlarmId == glider.FlarmId);
                    gliderList.Add(new GliderListItem
                    {
                        Owner = glider.Owner,
                        Registration = glider.Registration,
                        RegistrationShort = glider.RegistrationShort,
                        Model = glider.Model,
                        TakeOffTimestamp = flightbookEntry == null ? -1 : (long)flightbookEntry.TakeOffTimestamp.Value * (long)1000,
                        Status = gliderStatus,
                        Pilot = "Not implemented",
                        DistanceFromHome = distanceFromHome,
                        Altitude = (int)flight.HeightMSL,
                        FlarmId = glider.FlarmId,
                        Timestamp = flight.Timestamp,
                        Longitude = flight.Longitude,
                        Latitude = flight.Latitude,
                    });;
                }
            }
            gliderList.Sort();
            return Ok(gliderList);
        }

        [HttpGet("flightbook/loxn")]
        public async Task<ActionResult<GetFlightbookResponse>> GetLoxnFlightbook()
        {
            var response = await _httpClient.GetFromJsonAsync<GetFlightbookResponse>(_flightBookUrl);
            return Ok(response);
        }

        [HttpGet("flights/{flarmId}/path")]
        public async Task<ActionResult<string>> GetFlightPath(string flarmId)
        {
            string url = $"https://api.glideandseek.com/v2/track/{flarmId}";
            var response = await _httpClient.GetFromJsonAsync<GetOgnFlightPathResponse>(url);
            if (response != null && response.Success)
            {
                ;
                return Ok(response.Message);
            }
            return BadRequest();
        }

        [HttpGet("flights/{flarmId}/history")]
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