namespace FlugsportWienOgnApi.Models.Flightbook;

public class GetFlightbookResponse
{
    public IEnumerable<GetFlightbookDeviceDto> devices { get; set; }
    public IEnumerable<GetFlightbookFlightsDto> flights { get; set; }
}
