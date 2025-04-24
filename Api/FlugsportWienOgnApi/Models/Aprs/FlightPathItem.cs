using FlugsportWienOgnApi.Models.Core;

namespace FlugsportWienOgnApi.Models.Aprs;

public class FlightPathItem
{
    public Coordinate Location { get; set; }
    public int Altitude { get; set; }
    public int Speed { get; set; }
    public float VerticalSpeed { get; set; }
    public DateTime Timestamp { get; set; }
}
