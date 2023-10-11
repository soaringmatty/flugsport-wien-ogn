namespace FlugsportWienOgnApi.Models.Core;

public class FlightEvent
{
    public DateTime Timestamp { get; set; }
    public FlightEventType FlightEventType { get; set; }
    public bool IsWinchLaunch { get; set; } = false;
    public bool IsLaunchMethodChecked { get; set; } = false;
}
