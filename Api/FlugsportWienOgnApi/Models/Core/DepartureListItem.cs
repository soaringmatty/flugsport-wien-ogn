namespace FlugsportWienOgnApi.Models.Core;

public class DepartureListItem
{
    public string FlarmId { get; set; }
    public string Registration { get; set; }
    public string RegistrationShort { get; set; }
    public string Model { get; set; }
    public long? TakeOffTimestamp { get; set; }
    public long? LandingTimestamp { get; set; }
}
