using FlugsportWienOgn.Database;
using FlugsportWienOgnApi.Models.Core;
using FlugsportWienOgnApi.Models.Flightbook;
using FlugsportWienOgnApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlugsportWienOgnApi.Controllers;

[Route("flightbook")]
[ApiController]
public class FlightbookController(ILogger<FlightController> logger, IHttpClientFactory httpClientFactory, KnownAircraftService knownAircraftService, IServiceProvider serviceProvider, FlightbookService flightbookService) : ControllerBase
{
    private readonly string _flightBookUrl = "https://flightbook.glidernet.org/api/logbook/LOXN/";

    [HttpGet("loxn")]
    public async Task<ActionResult<IEnumerable<DepartureListItem>>> GetLoxnInternalFlightbook([FromQuery] bool? knownGlidersOnly = false)
    {
        var departureList = await flightbookService.GetLoxnFlightbook(knownGlidersOnly);
        return Ok(departureList);
    }

    [HttpGet("loxn/glidernet")]
    public async Task<ActionResult<IEnumerable<DepartureListItem>>> GetLoxnFlightbook([FromQuery] bool? knownGlidersOnly = false)
    {
        var client = httpClientFactory.CreateClient();
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
                TakeOffTimestamp = flight.start_tsp.HasValue ? DateTimeOffset.FromUnixTimeSeconds(flight.start_tsp.Value) : null,
                LandingTimestamp = flight.stop_tsp.HasValue ? DateTimeOffset.FromUnixTimeSeconds(flight.stop_tsp.Value) : null
            });

        var departureList =
            from flightBook in joinedFlightbook
            join glider in knownAircraftService.AllKnownPlanes on flightBook.FlarmId equals glider.FlarmId
            select new DepartureListItem
            {
                FlarmId = flightBook.FlarmId,
                Registration = glider.Registration,
                RegistrationShort = glider.RegistrationShort,
                Model = glider.Model,
                DepartureTimestamp = flightBook.TakeOffTimestamp,
                LandingTimestamp = flightBook.LandingTimestamp,
                LaunchType = (glider.AircraftType != (int)AircraftType.Glider) ? LaunchType.Motorized : LaunchType.Winch,
                //LaunchHeight = null
            };
        departureList = departureList.OrderByDescending(item => item.DepartureTimestamp);

        return Ok(departureList);
    }
}
