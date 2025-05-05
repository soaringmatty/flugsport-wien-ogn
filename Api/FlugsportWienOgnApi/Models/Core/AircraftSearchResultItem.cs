namespace FlugsportWienOgnApi.Models.Core;

public class AircraftSearchResultItem
{
    public string FlarmId { get; set; }
    public string Registration { get; set; }
    public string RegistrationShort { get; set; }
    public string Model { get; set; }
    public GliderOwnership Type { get; set; }
    public AircraftType AircraftType { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public long Timestamp { get; set; }
    public int Priority { get; set; }
    public int MatchRank { get; set; }
}
