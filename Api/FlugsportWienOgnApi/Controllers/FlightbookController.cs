using FlugsportWienOgnApi.Models.Core;
using FlugsportWienOgnApi.Models.Flightbook;
using FlugsportWienOgnApi.Services;
using FlugsportWienOgnApi.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace FlugsportWienOgnApi.Controllers;

[Route("flightbook")]
[ApiController]
public class FlightbookController : ControllerBase
{
    private readonly ILogger<FlightController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    //private readonly LoxnFlightbookService _loxnFlightbookService;
    private readonly KnownAircraftService _knownAircraftService;
    private readonly string _flightBookUrl = "https://flightbook.glidernet.org/api/logbook/LOXN/";

    public FlightbookController(ILogger<FlightController> logger, IHttpClientFactory httpClientFactory, KnownAircraftService knownAircraftService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        //_loxnFlightbookService = loxnFlightbookService;
        _knownAircraftService = knownAircraftService;
    }

    [HttpGet("loxn")]
    public async Task<ActionResult<IEnumerable<DepartureListItem>>> GetLoxnFlightbook([FromQuery] bool? knownGlidersOnly = false)
    {
        var client = _httpClientFactory.CreateClient();
        var getFlightbookResponse = await client.GetFromJsonAsync<GetFlightbookResponse>(_flightBookUrl);
        if (getFlightbookResponse == null)
        {
            return BadRequest($"Failed to retrieve flightbook data from {_flightBookUrl}");
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

        var departureList =
            from flightBook in joinedFlightbook
            join glider in _knownAircraftService.AllKnownPlanes on flightBook.FlarmId equals glider.FlarmId
            select new DepartureListItem
            {
                FlarmId = flightBook.FlarmId,
                Registration = glider.Registration,
                RegistrationShort = glider.RegistrationShort,
                Model = glider.Model,
                DepartureTimestamp = flightBook.TakeOffTimestamp * 1000 ?? null,
                LandingTimestamp = flightBook.LandingTimestamp * 1000 ?? null,
                LaunchType = (glider.AircraftType != (int)AircraftType.Glider) ? LaunchType.Motorized : LaunchType.Winch,
                //LaunchHeight = null
            };
        departureList = departureList.OrderByDescending(item => item.DepartureTimestamp);

        return Ok(departureList);
    }

    //[HttpGet("loxn/new")]
    //[Obsolete]
    //public ActionResult<IEnumerable<DepartureListItem>> GetLoxnFlightbookNew([FromQuery] bool includePrivateGliders)
    //{
    //    var knownGliders = includePrivateGliders ? _knownAircraftService.ClubAndPrivateGliders : _knownAircraftService.ClubGliders;

    //    var departureList = _loxnFlightbookService.FlightBook.OrderByDescending(item => item.TakeOffTimestamp);
    //    return Ok(departureList);
    //}
}
