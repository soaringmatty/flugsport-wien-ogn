namespace FlugsportWienOgnApi.Models.Flightbook;

public class GetFlightbookJoinResult
{
    public required string FlarmId { get; set; }
    public DateTimeOffset? TakeOffTimestamp { get; set; }
    public DateTimeOffset? LandingTimestamp { get; set; }
}
