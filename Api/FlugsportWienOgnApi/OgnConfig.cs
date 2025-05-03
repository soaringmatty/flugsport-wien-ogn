namespace FlugsportWienOgnApi;

public class OgnConfig
{
    /// <summary>
    /// If enabled, flight data outside of austrian airspace will be ignored and not stored in the database
    /// </summary>
    public bool AustrianAirspaceOnly { get; set; } = true;

    /// <summary>
    /// Max age of the last received position data in order for the aircraft to be included in GetFlights response.
    /// (Minutes)
    /// </summary>
    public int FlightDataMaxAge { get; set; } = 30;
}
