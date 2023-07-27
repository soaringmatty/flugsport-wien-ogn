namespace FlugsportWienOgnApi.Models.Core;

public class Flight
{
    public string FlarmId { get; set; }
    public string DisplayName { get; set; }
    public string Registration { get; set; }
    public GliderType Type { get; set; }
    public AircraftType AircraftType { get; set; }
    public string Model { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public float HeightMSL { get; set; }
    public float HeightAGL { get; set; }
    public long Timestamp { get; set; }
    public float Speed { get; set; }
    public float Vario { get; set; }
    public float VarioAverage { get; set; }
}
