using FlugsportWienOgnApi.Models.Core;

namespace FlugsportWienOgnApi.Models.LiveTracking;

public class FlightPathItemDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Altitude { get; set; }
    public int Speed { get; set; }
    public float VerticalSpeed { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
