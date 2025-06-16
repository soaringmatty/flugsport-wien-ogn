namespace FlugsportWienOgnApi.Models.Core;

public class DepartureListItem
{
    public required string FlarmId { get; set; }
    public required string Registration { get; set; }
    public required string RegistrationShort { get; set; }
    public required string Model { get; set; }
    public DateTimeOffset? DepartureTimestamp { get; set; }
    public DateTimeOffset? LandingTimestamp { get; set; }
    public LaunchType LaunchType { get; set ; }
    public int? LaunchHeight { get; set; }
}
