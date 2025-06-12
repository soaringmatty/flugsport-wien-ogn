namespace FlugsportWienOgnApi.Models.Core;

public class DepartureListItem
{
    public string FlarmId { get; set; }
    public string Registration { get; set; }
    public string RegistrationShort { get; set; }
    public string Model { get; set; }
    public long? DepartureTimestamp { get; set; }
    public long? LandingTimestamp { get; set; }
    public LaunchType LaunchType { get; set ; }
    public int? LaunchHeight { get; set; }
}
