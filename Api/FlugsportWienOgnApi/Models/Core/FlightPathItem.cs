using Arps;

namespace FlugsportWienOgnApi.Models.Core;

public class FlightPathItem
{
    public Coordinate Location { get; set; }
    public int Altitude { get; set; }
    public int Speed { get; set; }
    public float VerticalSpeed { get; set; }
    public DateTime Timestamp { get; set; }
}
