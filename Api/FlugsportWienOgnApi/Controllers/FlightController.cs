using FlugsportWienOgnApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Text.Json;
using FlugsportWienOgnApi.Models.GlideAndSeek;

namespace FlugsportWienOgnApi.Controllers
{
    [ApiController]
    [Route("flights")]
    public class FlightController : ControllerBase
    {
        private readonly ILogger<FlightController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;

        public FlightController(ILogger<FlightController> logger, IHttpClientFactory httpClientFactory, HttpClient httpClient)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _httpClient = httpClient;
        }

        //[HttpGet]
        //public async Task<IEnumerable<GetFlightsResponse>> GetFlights()
        //{
        //    var httpRequestMessage = new HttpRequestMessage(
        //        HttpMethod.Get,
        //        "https://api.glideandseek.com/v2/aircraft?showAllTracks=false&a=48.72&b=46.65&c=16.70&d=11.00");

        //    var httpClient = _httpClientFactory.CreateClient();
        //    var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

        //    IEnumerable<GetFlightsResponse> result = new List<GetFlightsResponse>();
        //    if (httpResponseMessage.IsSuccessStatusCode)
        //    {
        //        Console.WriteLine(await httpResponseMessage.Content.ReadAsStringAsync());
        //        _logger.LogInformation("Flights", await httpResponseMessage.Content.ReadAsStringAsync());
        //        _logger.LogInformation("Flights", await httpResponseMessage.Content.ReadFromJsonAsync<IEnumerable>());
        //        //using var contentStream =
        //        //    await httpResponseMessage.Content.ReadAsStreamAsync();

        //        //result = await JsonSerializer.DeserializeAsync<IEnumerable<GetFlightsResponse>>(contentStream);
        //    }
        //}

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GlideAndSeekFlight>>> GetFlights()
        {
            string url = "https://api.glideandseek.com/v2/aircraft?showAllTracks=false&a=48.72&b=46.65&c=16.70&d=11.00";
            var response = await _httpClient.GetFromJsonAsync<GetOgnFlightsResponse>(url);
            if (response != null && response.Success)
            {
                var flights = MapToFlights(response.Message);
                return Ok(flights);
            }

            return BadRequest();
        }

        private IEnumerable<GlideAndSeekFlight> MapToFlights(IEnumerable<GetOgnFlightsResponseDto> rawFlights)
        {
            List<GlideAndSeekFlight> flights = new();
            foreach (var rawFlight in rawFlights)
            {
                flights.Add(new GlideAndSeekFlight
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