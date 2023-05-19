namespace FlugsportWienOgnApi.Models.Flightbook;

public class GetFlightbookFlightsDto
{
    public int device { get; set; }
    public long? start_tsp { get; set; }
    public long? stop_tsp { get; set; }

}
