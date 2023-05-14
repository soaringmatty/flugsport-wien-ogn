using Microsoft.AspNetCore.Mvc;
using FlugsportWienOgnApi.Models.GlideAndSeek;
using FlugsportWienOgnApi.Models.Core;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights([FromQuery] bool showAllGliders = false, [FromQuery] bool showPrivateGliders = false)
        {
            string url = "https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a=49&b=17.20&c=46.60&d=9.40";
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

        [HttpGet("{flarmId}/history")]
        public async Task<ActionResult<IEnumerable<FlightHistoryItem>>> GetFlightHistory(string flarmId)
        {
            Console.WriteLine("Loading");
            string url = $"https://api.glideandseek.com/v2/history/{flarmId}";
            var response = await _httpClient.GetFromJsonAsync<GetFlightHistoryResponse>(url);
            if (response == null || !response.Success)
            {
                return BadRequest();
            }

            Console.WriteLine("Mapping");
            var flightHistoryList = new List<FlightHistoryItem>();
            foreach (var historyItem in response.Message)
            {
                var flightData = new FlightHistoryItem
                {
                    Timestamp = Convert.ToInt64(historyItem[0].ToString()),
                    Unknown = double.Parse(historyItem[1].ToString(), CultureInfo.InvariantCulture.NumberFormat),
                    Latitude = double.Parse(historyItem[2].ToString(), CultureInfo.InvariantCulture.NumberFormat),
                    Longitude = double.Parse(historyItem[3].ToString(), CultureInfo.InvariantCulture.NumberFormat),
                    Altitude = double.Parse(historyItem[4].ToString(), CultureInfo.InvariantCulture.NumberFormat),
                    GroundHeight = double.Parse(historyItem[5].ToString(), CultureInfo.InvariantCulture.NumberFormat),
                };

                flightHistoryList.Add(flightData);
            }
            return flightHistoryList;
        }



        [HttpGet("{flarmId}/history/raw")]
        public async Task<ActionResult<JArray>> GetFlightHistoryRaw(string flarmId)
        {
            Console.WriteLine("Loading");
            string url = $"https://api.glideandseek.com/v2/history/{flarmId}";
            //var response = await _httpClient.GetFromJsonAsync<GetFlightHistoryResponse>(url);
            //if (response == null || !response.Success || response.Message == null)
            //{
            //    return BadRequest();
            //}

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
                var success = Convert.ToBoolean(data["success"]);

                if (!success)
                {
                    return BadRequest("External API request failed.");
                }

                var message = data["message"] as JArray;
                return message;
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