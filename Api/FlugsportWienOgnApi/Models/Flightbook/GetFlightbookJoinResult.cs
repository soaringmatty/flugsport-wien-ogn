namespace FlugsportWienOgnApi.Models.Flightbook;

public class GetFlightbookJoinResult
{
    public string FlarmId { get; set; }
    public long? TakeOffTimestamp { get; set; }
    public long? LandingTimestamp { get; set; }
}
