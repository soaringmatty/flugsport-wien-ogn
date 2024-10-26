namespace FlugsportWienOgnApi.Models.Aprs;

public class LiveFlight
{
    public List<FlightPathItem> FlightPath { get; set; }
    public FlightStatus FlightStatus { get; set; }
    public DateTime? FlightStatusChangedTime { get; set; }

    public LiveFlight()
    {
        FlightPath = new List<FlightPathItem>();
        FlightStatus = FlightStatus.NoSignal;
    }
}
