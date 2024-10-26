namespace Arps;

/// <summary>
/// Representation of current configuration.
/// Will automatically instantiated by the ConfigProvider
/// </summary>
public record AprsConfig
{
    /// <summary>
    /// APRS host that the listener should connect to
    /// </summary>
    public string AprsHost { get; init; } = "aprs.glidernet.org";

    /// <summary>
    /// APRS port that the listener should connect to
    /// </summary>
    public int AprsPort { get; init; } = 14580;

    /// <summary>
    /// Username that will be used while authenticating to the APRS server
    /// </summary>
    public string AprsUser { get; init; } = "fswogn";

    /// <summary>
    /// Password that will be used while authenticating to the APRS server
    /// </summary>
    public string AprsPassword { get; init; } = "-1";

    /// <summary>
    /// Url that contains the list of all OGN-known aircraft (OGN DDB)
    /// </summary>
    public string DdbAircraftListUrl { get; init; } = "https://ddb.glidernet.org/download/?t=1";

    /// <summary>
    /// Position that should be listened for
    /// </summary>
    public double FilterPositionLatitude { get; init; }
    public double FilterPositionLongitude { get; init; }

    /// <summary>
    /// Radius around the FilterPosition that should be listened for in km.
    /// </summary>
    public int FilterRadius { get; init; } = 100;
}