using FlugsportWienOgnApi.Models.Core;
using FlugsportWienOgnApi.Models.Flightbook;
using FlugsportWienOgnApi.Models.GlideAndSeek;
using FlugsportWienOgnApi.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace FlugsportWienOgnApi.Controllers;

[Route("gliders")]
[ApiController]
public class GliderStatusController : ControllerBase
{
    private readonly ILogger<GliderStatusController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly string _liveFlightsUrl = "https://api.glideandseek.com/v2/aircraft?showOnlyGliders=true&a=52&b=22&c=43&d=7";
    private readonly string _flightBookUrl = "https://flightbook.glidernet.org/api/logbook/LOXN/";

    public GliderStatusController(ILogger<GliderStatusController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("status")]
    public async Task<ActionResult<IEnumerable<GliderListItem>>> GetGliderStatusList([FromQuery] bool includePrivateGliders)
    {
        List<GliderListItem> gliderStatusList = new List<GliderListItem>();
        List<Flight> flights = new List<Flight>();

        var client = _httpClientFactory.CreateClient();
        var getFlightsResponse = await client.GetFromJsonAsync<GetOgnFlightsResponse>(_liveFlightsUrl);
        if (getFlightsResponse != null && getFlightsResponse.Success)
        {
            flights = Mapping.MapOgnFlightsResponseToFlights(getFlightsResponse.Message).ToList();
        }

        var getFlightbookResponse = await client.GetFromJsonAsync<GetFlightbookResponse>(_flightBookUrl);
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
        var knownGliders = includePrivateGliders ? KnownGliders.ClubAndPrivateGliders : KnownGliders.ClubGliders;
        foreach (var glider in knownGliders)
        {
            var flight = flights.FirstOrDefault(x => x.FlarmId == glider.FlarmId);
            if (flight == null)
            {
                // Keep private gliders without a signal out of the result
                if (includePrivateGliders && KnownGliders.PrivateGliders.Any(x => x.FlarmId == glider.FlarmId))
                {
                    continue;
                }
                gliderStatusList.Add(new GliderListItem
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
                gliderStatusList.Add(new GliderListItem
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
                }); ;
            }
        }
        gliderStatusList.Sort();
        return Ok(gliderStatusList);
    }
}
