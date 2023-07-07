using FlugsportWienOgnApi.Models.Core;
using FlugsportWienOgnApi.Models.Flightbook;
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
    private readonly string _flightBookUrl = "https://flightbook.glidernet.org/api/logbook/LOXN/";

    public FlightbookController(ILogger<FlightController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("loxn")]
    public async Task<ActionResult<IEnumerable<DepartureListItem>>> GetLoxnFlightbook([FromQuery] bool includePrivateGliders)
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

        var knownGliders = includePrivateGliders ? KnownGliders.ClubAndPrivateGliders() : KnownGliders.ClubGliders;

        var departureList =
            from flightBook in joinedFlightbook
            join glider in knownGliders on flightBook.FlarmId equals glider.FlarmId
            select new DepartureListItem
            {
                FlarmId = flightBook.FlarmId,
                Registration = glider.Registration,
                RegistrationShort = glider.RegistrationShort,
                Model = glider.Model,
                TakeOffTimestamp = flightBook.TakeOffTimestamp ?? 0,
                LandingTimestamp = flightBook.LandingTimestamp ?? 0
            };
        departureList = departureList.OrderByDescending(item => item.TakeOffTimestamp);

        //DEMO
        //departureList = new List<DepartureListItem>() {
        //    new DepartureListItem
        //    {
        //        FlarmId = "TEST01",
        //        Registration = "D-TST1",
        //        RegistrationShort = "T1",
        //        Model = "Test",
        //        TakeOffTimestamp = 1688467278,
        //        LandingTimestamp = 1688469980
        //    },
        //    new DepartureListItem
        //    {
        //        FlarmId = "TEST02",
        //        Registration = "D-TST2",
        //        RegistrationShort = "T2",
        //        Model = "Test",
        //        TakeOffTimestamp = 1688462280,
        //        LandingTimestamp = null
        //    },
        //    new DepartureListItem
        //    {
        //        FlarmId = "TEST03",
        //        Registration = "D-TST3",
        //        RegistrationShort = "T3",
        //        Model = "Test",
        //        TakeOffTimestamp = 1688461244,
        //        LandingTimestamp = 1688466937
        //    },
        //    new DepartureListItem { FlarmId = "TEST04", Registration = "D-TST4", RegistrationShort = "T4", Model = "Test", TakeOffTimestamp = 1688462400, LandingTimestamp = null },
        //    new DepartureListItem { FlarmId = "TEST05", Registration = "D-TST5", RegistrationShort = "T5", Model = "Test", TakeOffTimestamp = 1688462500, LandingTimestamp = 1688464000 },
        //    new DepartureListItem { FlarmId = "TEST06", Registration = "D-TST6", RegistrationShort = "T6", Model = "Test", TakeOffTimestamp = 1688462600, LandingTimestamp = null },
        //    new DepartureListItem { FlarmId = "TEST07", Registration = "D-TST7", RegistrationShort = "T7", Model = "Test", TakeOffTimestamp = 1688462700, LandingTimestamp = 1688464300 },
        //    new DepartureListItem { FlarmId = "TEST08", Registration = "D-TST8", RegistrationShort = "T8", Model = "Test", TakeOffTimestamp = 1688462800, LandingTimestamp = null },
        //    new DepartureListItem { FlarmId = "TEST09", Registration = "D-TST9", RegistrationShort = "T9", Model = "Test", TakeOffTimestamp = 1688462900, LandingTimestamp = 1688464500 },
        //    new DepartureListItem { FlarmId = "TEST10", Registration = "D-TS10", RegistrationShort = "T10", Model = "Test", TakeOffTimestamp = 1688463000, LandingTimestamp = null },
        //    new DepartureListItem { FlarmId = "TEST11", Registration = "D-TS11", RegistrationShort = "T11", Model = "Test", TakeOffTimestamp = 1688463100, LandingTimestamp = 1688464600 },
        //    new DepartureListItem { FlarmId = "TEST12", Registration = "D-TS12", RegistrationShort = "T12", Model = "Test", TakeOffTimestamp = 1688463200, LandingTimestamp = null },
        //    new DepartureListItem { FlarmId = "TEST13", Registration = "D-TS13", RegistrationShort = "T13", Model = "Test", TakeOffTimestamp = 1688463300, LandingTimestamp = 1688464800 }
        //};

        return Ok(departureList);
    }
}
