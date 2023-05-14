namespace FlugsportWienOgnApi.Models.Core;

public class FlightHistoryItem
{
    public long Timestamp { get; set; }
    public double Unknown { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double GroundHeight { get; set; }
}
