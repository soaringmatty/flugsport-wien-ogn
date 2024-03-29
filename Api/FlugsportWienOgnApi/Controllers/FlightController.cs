﻿using Microsoft.AspNetCore.Mvc;
using FlugsportWienOgnApi.Models.GlideAndSeek;
using FlugsportWienOgnApi.Models.Core;
using Newtonsoft.Json.Linq;
using FlugsportWienOgnApi.Utils;
using FlugsportWienOgnApi.Services;

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

        public FlightController(ILogger<FlightController> logger, HttpClient httpClient, IHttpClientFactory httpClientFactory, FlightService flightService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
            _flightService = flightService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights([FromQuery] string? selectedFlarmId, [FromQuery] bool? glidersOnly, [FromQuery] bool? clubGlidersOnly, [FromQuery] double? maxLat, [FromQuery] double? minLat, [FromQuery] double? maxLng, [FromQuery] double? minLng)
        {
            var flights = await _flightService.GetFlights(selectedFlarmId, glidersOnly, clubGlidersOnly, maxLat, minLat, maxLng, minLng);
            if (flights != null)
            {
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
            {
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
    }
}