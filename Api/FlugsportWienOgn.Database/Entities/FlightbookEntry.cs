using System.ComponentModel.DataAnnotations.Schema;

namespace FlugsportWienOgn.Database.Entities;

public class FlightbookEntry
{
    public int Id { get; set; }
    public required int AircraftId { get; set; }
    public DateTime? TakeOffTimestamp { get; set; }
    public DateTime? LandingTimestamp { get; set; }
    public required int LaunchType { get; set; }
    public int? LaunchHeight { get; set; }
    public required string AirfieldIcao { get; set; }
    public int? TowFlightEntryId { get; set; }


    public Aircraft Aircraft { get; set; }

    [ForeignKey(nameof(TowFlightEntryId))]
    public FlightbookEntry? TowFlightEntry { get; set; }
}
