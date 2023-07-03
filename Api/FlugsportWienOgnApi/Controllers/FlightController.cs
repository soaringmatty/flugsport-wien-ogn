using Microsoft.AspNetCore.Mvc;
using FlugsportWienOgnApi.Models.GlideAndSeek;
using FlugsportWienOgnApi.Models.Core;
using Newtonsoft.Json.Linq;
using FlugsportWienOgnApi.Utils;
using FlugsportWienOgnApi.Models.Flightbook;

namespace FlugsportWienOgnApi.Controllers
{
    [Route("flights")]
    [ApiController]
    public class FlightController : ControllerBase
    {
        private readonly ILogger<FlightController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _flightBookUrl = "https://flightbook.glidernet.org/api/logbook/LOXN/";

        public FlightController(ILogger<FlightController> logger, HttpClient httpClient, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights()
        {
            string url = $"https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a=49&b=17.2&c=46.6&d=9.4";
            //string url = $"https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a={maxLat}&b={maxLng}&c={minLat}&d={minLng}";
            var response = await _httpClient.GetFromJsonAsync<GetOgnFlightsResponse>(url);
            if (response != null && response.Success)
            {
                var flights = Mapping.MapOgnFlightsResponseToFlights(response.Message);
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
                ;
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