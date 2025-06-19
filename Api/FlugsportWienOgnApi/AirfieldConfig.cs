namespace FlugsportWienOgnApi;

public class AirfieldConfig
{
    public required string Icao { get; set; }
    public required int GroundHeight { get; set; }
    public required double MinLat { get; set; }
    public required double MaxLat { get; set; }
    public required double MinLng { get; set; }
    public required double MaxLng { get; set; }
}
