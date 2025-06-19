using FlugsportWienOgnApi.Models.Core;
using FlugsportWienOgnApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlugsportWienOgnApi.Controllers;

[Route("flightbook")]
[ApiController]
public class FlightbookController(ILogger<FlightController> logger, FlightbookService flightbookService) : ControllerBase
{
    [HttpGet("{icao}")]
    public async Task<ActionResult<IEnumerable<DepartureListItem>>> GetLoxnInternalFlightbook([FromRoute] string icao, [FromQuery] bool? knownGlidersOnly = false)
    {
        var departureList = await flightbookService.GetFlightbookByAirfieldIcao(icao, knownGlidersOnly);
        return Ok(departureList);
    }
}
