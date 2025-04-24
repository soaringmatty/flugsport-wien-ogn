using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Aprs.Models;
using Microsoft.Extensions.Logging;

namespace Aprs;

/// <summary>
/// Converter for getting models from stream data messages
/// </summary>
public partial class StreamConverter
{
    private readonly ILogger<StreamConverter> _logger;
    /// <summary>
    /// Main pattern for getting all needed information from a raw stream-line.
    /// </summary>
    /// <remarks>
    /// Note that at the part of "idXXYYYYYY", "XX" must not be 40 or higher!
    /// This is due to the fact that this 2-digit hex number contains the tracking-information as _binary_ in the
    /// form of "0bSTxxxxxx" and if S = 1 or T = 1, we should discard the message.
    /// So all "allowed" values are in the range of 0b00000000 - 0b00111111, or in hex: 0x00 - 0x3f,
    /// therefore we can discard all messages not in this range.
    /// <seealso href="https://github.com/dbursem/ogn-client-php/blob/master/lib/OGNClient.php#L87"/>
    /// </remarks>
    private const string _LINE_MATCH_PATTERN =
        @".*?([A-Za-z0-9]+,[A-Za-z0-9]+,[A-Za-z0-9]+)"  // 1: receiver identifier
      + @".*?([0-9]{6}h)"                               // 2: timestamp (DDHHMMh or HHMMSSz)
      + @"([0-9.]*[NS])[/\\]([0-9.]*[WE])"              // 3: latitude, 4: longitude
      + @".*?(\d{3})/(\d{3})/A=(\d+)"                   // 5: course (°), 6: speed (kn), 7: altitude (ft)
      + @".*?id[0-3]{1}[A-Fa-f0-9]{1}([A-Za-z0-9]+)"    // 8: aircraft ID
      + @".*?([-0-9]+)fpm"                              // 9: vertical speed (ft/min)
      + @".*?([-.0-9]+)rot.*";                          // 10: turn rate (turns per 2 min)

    /// <summary>
    /// Pattern for converting coordinate strings to valid numeric string
    /// (aka "remove all non-numeric chars")
    /// </summary>
    private const string _COORDINATE_REPLACE_PATTERN = @"[^\d]";

    /// <summary>
    /// Factor to convert knots to km/h
    /// </summary>
    private const float _FACTOR_KNOTS_TO_KM_H = 1.852f;

    /// <summary>
    /// Factor to convert ft to m
    /// </summary>
    private const float _FACTOR_FT_TO_M = 0.3048f;

    /// <summary>
    /// Factor to convert ft/min to m/s 
    /// </summary>
    private const float _FACTOR_FT_MIN_TO_M_SEC = 0.00508f;

    /// <summary>
    /// Factor to convert "turns/2min" to "turns/min"
    /// </summary>
    private const float _FACTOR_TURNS_TWO_MIN_TO_TURNS_MIN = 0.5f;

    [GeneratedRegex(_COORDINATE_REPLACE_PATTERN)]
    private static partial Regex CoordinateReplaceRegex();

    public StreamConverter(ILogger<StreamConverter> logger)
    {
        _logger = logger;   
    }

    /// <summary>
    /// Tries converting a stream-line to FlightData model
    /// </summary>
    /// <param name="line">A line that was received by the OGN live stream</param>
    /// <returns>FlightData representation of the data</returns>
    public FlightData? ConvertData(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            _logger.LogError("Line is null or empty!");
            return null;
        }

        var match = Regex.Match(line, _LINE_MATCH_PATTERN);
        if (!match.Success)
        {
            return null;
        }

        var data = match.Groups;

        var receiver = data[1].Value;
        var timestamp = ConvertTimestamp(data, 2).ToLocalTime();
        var latitude = ConvertCoordinateValue(data, 3);
        var longitude = ConvertCoordinateValue(data, 4);
        var course = Convert(data, 5);
        var speed = Convert(data, 6, _FACTOR_KNOTS_TO_KM_H);
        var altitude = Convert(data, 7, _FACTOR_FT_TO_M);
        var aircraftId = data[8].Value;
        var verticalSpeed = Convert(data, 9, _FACTOR_FT_MIN_TO_M_SEC);
        var turnRate = Math.Abs(Convert(data, 10, _FACTOR_TURNS_TWO_MIN_TO_TURNS_MIN));

        return new FlightData(
            aircraftId,
            speed, altitude,
            verticalSpeed,
            turnRate,
            course,
            latitude,
            longitude,
            timestamp,
            DateTime.Now,
            receiver
        );
    }

    /// <summary>
    /// Converts the match-result from the regex to a float and multiplies it with a given factor to convert units
    /// </summary>
    /// <param name="collection">Result of the regex match</param>
    /// <param name="index">Result index</param>
    /// <param name="factor">The factor that should be applied to the value</param>
    /// <returns></returns>
    private static float Convert(GroupCollection collection, int index, float factor = 1)
    {
        var value = collection[index].Value;
        var doubleValue = double.Parse(value, CultureInfo.InvariantCulture);
        return (float)doubleValue * factor;
    }

    /// <summary>
    /// Converts the strings representing coordinate values to a common float representation
    /// </summary>
    /// <param name="collection">Result of the regex match</param>
    /// <param name="index">Result index</param>
    /// <returns></returns>
    private static float ConvertCoordinateValue(GroupCollection collection, int index)
    {
        /* Latitude and longitude (by APRS-standard) are given as following: ddmm.mmD where d = "degree",
         * m = "minute" and D = "direction".
         * Notice that minutes are decimals, so 0.5 minutes equal 0 minutes, 30 secs.
         * We'll separate degrees and minutes, so we can convert it to a "degree"-only value.
         */
        var rawValue = collection[index].Value;

        var numericValue = System.Convert.ToDouble(CoordinateReplaceRegex().Replace(rawValue, string.Empty));
        var orientation = rawValue[^1..];

        var degrees = Math.Floor(numericValue / 1_0000); // Separating   "dd" from "ddmmmm"
        var minutes = Math.Floor(numericValue % 1_0000) // Separating "mmmm" from "ddmmmm"
                      / 60 // because 60 minutes = 1 degree
                      / 100; // because of the removed decimal separator

        var coordinateValue = (float)(degrees + minutes);

        return orientation.Equals("S") || orientation.Equals("W")
            ? coordinateValue * -1 // S/W are seen as negative!
            : coordinateValue;
    }

    /// <summary>
    /// Converts an APRS timestamp in DDHHMMh (local) or HHMMSSz (UTC) format 
    /// into a DateTime instance.
    /// </summary>
    /// <param name="rawTimestamp">
    /// A seven-character APRS timestamp, e.g. "071645h" or "092345z".
    /// </param>
    /// <returns>
    /// A DateTime representing the parsed timestamp.
    /// </returns>
    private static DateTime ConvertTimestamp(GroupCollection collection, int index)
    {
        var rawTimestamp = collection[index].Value;
        if (string.IsNullOrWhiteSpace(rawTimestamp) || rawTimestamp.Length != 7)
        {
            throw new FormatException("Invalid APRS timestamp format.");
        }

        char suffix = rawTimestamp[^1];
        string core = rawTimestamp.Substring(0, 6);
        DateTime now = (suffix == 'z') ? DateTime.UtcNow : DateTime.Now;

        switch (suffix)
        {
            case 'h': // HHMMSS – hour, minute, second in UTC
                {
                    int hourZ = int.Parse(core.Substring(0, 2), CultureInfo.InvariantCulture);
                    int minuteZ = int.Parse(core.Substring(2, 2), CultureInfo.InvariantCulture);
                    int secondZ = int.Parse(core.Substring(4, 2), CultureInfo.InvariantCulture);

                    DateTime utcNow = DateTime.UtcNow;
                    // UTC DateTime is constructed with Kind=Utc
                    return new DateTime(
                        utcNow.Year,
                        utcNow.Month,
                        utcNow.Day,
                        hourZ,
                        minuteZ,
                        secondZ,
                        DateTimeKind.Utc
                    );
                }

            default:
                throw new FormatException($"Unknown timestamp suffix: '{suffix}'.");
        }
    }
}