using FlugsportWienOgnApi.Models.Core;

namespace FlugsportWienOgnApi.Models.LiveTracking;

public class FlightPathItemDto
{
    public Coordinate Location { get; set; }
    public int Altitude { get; set; }
    public int Speed { get; set; }
    public float VerticalSpeed { get; set; }
    public DateTime Timestamp { get; set; }
    public string Receiver { get; set; }
}
