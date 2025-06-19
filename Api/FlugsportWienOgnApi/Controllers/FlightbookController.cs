using FlugsportWienOgnApi.Models.Core;
using FlugsportWienOgnApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlugsportWienOgnApi.Controllers;

[Route("flightbook")]
[ApiController]
public class FlightbookController(FlightbookService flightbookService) : ControllerBase
{
    [HttpGet("{icao}")]
    public async Task<ActionResult<IEnumerable<DepartureListItem>>> GetLoxnInternalFlightbook([FromRoute] string icao, [FromQuery] GliderListFilter filter)
    {
        var departureList = await flightbookService.GetFlightbookByAirfieldIcao(icao, filter);
        return Ok(departureList);
    }
}
