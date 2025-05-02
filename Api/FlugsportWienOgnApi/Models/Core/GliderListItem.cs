using FlugsportWienOgnApi.Models.Aprs;

namespace FlugsportWienOgnApi.Models.Core;

/// <summary>
/// Represents a status list item of a known glider including data like TakeOffTime and DistanceFromHome
/// </summary>
public class GliderListItem : IComparable<GliderListItem>
{
    public string Registration { get; set; }
    public string RegistrationShort { get; set; }
    public string Model { get; set; }
    public string Owner { get; set; }
    public string Pilot { get; set; }
    public FlightStatus Status { get; set; }
    public int DistanceFromHome { get; set; }
    public int Altitude { get; set; }
    public long TakeOffTimestamp { get; set; }
    public string FlarmId { get; set; }
    public long Timestamp { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }

    public int CompareTo(GliderListItem other)
    {
        if (other == null)
        {
            return 1;
        }

        // Firstly, compare by Status.
        int statusComparison = other.Status.CompareTo(this.Status);
        if (statusComparison != 0)
        {
            return statusComparison;
        }

        // If Status is equal, compare by FlightTime.
        int flightTimeComparison = this.TakeOffTimestamp.CompareTo(other.TakeOffTimestamp);
        if (flightTimeComparison != 0)
        {
            return flightTimeComparison;
        }

        // If FlightTime is also equal, compare by Model.
        return this.Model.CompareTo(other.Model);
    }
}

