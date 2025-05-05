namespace FlugsportWienOgnApi;

public class OgnConfig
{
    /// <summary>
    /// If enabled, flight data outside of austrian airspace will be ignored and not stored in the database
    /// </summary>
    public bool AustrianAirspaceOnly { get; set; } = true;

    /// <summary>
    /// Indicates, whether flight data of unregistered aircraft (that are not in FlarmNet database) should be tracked
    /// </summary>
    public bool IgnoreUnregisteredAircraft { get; set; } = false;

    /// <summary>
    /// Indicates, whether flight data of registered paragliders and hanggliders should be tracked
    /// </summary>
    public bool IgnoreParagliders { get; set; } = false;

    /// <summary>
    /// Max age of the last received position data in order for the aircraft to be included in GetFlights response.
    /// (Minutes)
    /// </summary>
    public int FlightDataMaxAge { get; set; } = 30;
}
