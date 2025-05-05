using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aprs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aprs;

/// <summary>
/// Provider for all OGN aircraft coming from the DDB
/// </summary>
public class AircraftProvider
{
    private const string _VALUE_YES = "Y";
    private const string _FIELD_ENCLOSURE = "'";
    private const char _FIELD_SEPARATOR = ',';
    private const char _IDENTIFIER_COMMENT = '#';
    private const char _LINE_BREAK = '\n';

    private const int _INDEX_AIRCRAFT_ID = 1;
    private const int _INDEX_MODEL = 2;
    private const int _INDEX_REGISTRATION = 3;
    private const int _INDEX_CALL_SIGN = 4;
    private const int _INDEX_TRACKED = 5;
    private const int _INDEX_IDENTIFIED = 6;
    private const int _INDEX_AIRCRAFT_TYPE = 7;

    private readonly Dictionary<string, Aircraft> _aircraftList = new Dictionary<string, Aircraft>();
    private readonly Dictionary<string, GlidernetAircraftType> _modelAircraftTypeDictionary = new Dictionary<string, GlidernetAircraftType>();
    private readonly AprsConfig _aprsConfig;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AircraftProvider> _logger;

    public AircraftProvider(HttpClient httpClient, ILogger<AircraftProvider> logger, IOptions<AprsConfig> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _aprsConfig = config.Value;
        InitializeModelAircraftTypeDictionary();
    }

    /// <summary>
    /// Initializes the provider and downloads / parses the data.
    /// Must be called before trying to Load any aircraft
    /// </summary>
    /// <returns>Task indicating whether initialization is done</returns>
    /// <seealso href="https://github.com/glidernet/ogn-ddb/blob/master/README.md"/>
    /// <exception cref="Exception">On invalid config or HTTP-errors</exception>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_aprsConfig.DdbAircraftListUrl))
        {
            throw new ArgumentNullException(nameof(_aprsConfig.DdbAircraftListUrl), "Missing URL in configuration.");
        }

        var client = new HttpClient();
        using var response = await _httpClient
            .GetAsync(_aprsConfig.DdbAircraftListUrl, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        _aircraftList.Clear();

        // Stream-based parsing to limit memory usage
        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (line.Length == 0 || line.StartsWith(_IDENTIFIER_COMMENT)) continue;

            var fields = line
                .Replace(_FIELD_ENCLOSURE, string.Empty)
                .Split(_FIELD_SEPARATOR);
            if (fields.Length < 7
                || string.IsNullOrWhiteSpace(fields[_INDEX_AIRCRAFT_ID]))
                //|| fields[_INDEX_TRACKED] != _VALUE_YES
                //|| fields[_INDEX_IDENTIFIED] != _VALUE_YES)
                continue;
            if (!int.TryParse(fields[_INDEX_AIRCRAFT_TYPE], NumberStyles.None, CultureInfo.InvariantCulture, out var type)) continue;

            var aircraft = new Aircraft(
                fields[_INDEX_AIRCRAFT_ID],
                fields[_INDEX_CALL_SIGN],
                fields[_INDEX_REGISTRATION],
                fields[_INDEX_MODEL],
                true,
                GetCorrectedAircraftType(type, fields[_INDEX_MODEL])
            );
            _aircraftList[aircraft.Id] = aircraft;
        }
    }

    /// <summary>
    /// Loads aircraft by given ID.
    /// If aircraft cannot be found, an empty Aircraft will be returned.
    /// </summary>
    /// <param name="aircraftId">OGN ID of the aircraft</param>
    /// <returns>Representation of an Aircraft</returns>
    public Aircraft? Load(string aircraftId)
    {
        if (string.IsNullOrEmpty(aircraftId))
        {
            return null;
        }
        return _aircraftList.TryGetValue(aircraftId, out var value)
            ? value
            : null;
    }

    private GlidernetAircraftType GetCorrectedAircraftType(int type, string model)
    {
        var aircraftType = (GlidernetAircraftType)type;
        if (_modelAircraftTypeDictionary.TryGetValue(model, out GlidernetAircraftType updatedType))
        {
            aircraftType = updatedType;
        }
        return aircraftType;
    }

    private void InitializeModelAircraftTypeDictionary()
    {
        // Actually TMGs
        _modelAircraftTypeDictionary.Add("AVo 68 Samburo", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("Carat", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("Grob G109", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("H36 Dimona", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("HK36 Super Dimona", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("L 13 SEH Vivat", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("Motorglider", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("RF 10", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("RF 3", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("RF 4", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("RF 5", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("RF 5 b", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("RF 9", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("SF-25", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("SF-28", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("SFS-31 Milan", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("Valentin Taifun", GlidernetAircraftType.Motorplane);

        // Other
        _modelAircraftTypeDictionary.Add("Hawker Hurricane", GlidernetAircraftType.Motorplane);
        _modelAircraftTypeDictionary.Add("Sinus", GlidernetAircraftType.Ultralight);
        _modelAircraftTypeDictionary.Add("SkyArrow", GlidernetAircraftType.Ultralight);
        _modelAircraftTypeDictionary.Add("Tetra-15", GlidernetAircraftType.Glider);

        // Unknown
        _modelAircraftTypeDictionary.Add("A380", GlidernetAircraftType.Unknown);
        _modelAircraftTypeDictionary.Add("Balloon", GlidernetAircraftType.Unknown); // Balloon
        _modelAircraftTypeDictionary.Add("Different Aircraft", GlidernetAircraftType.Unknown);
        _modelAircraftTypeDictionary.Add("Experimental", GlidernetAircraftType.Unknown);
        _modelAircraftTypeDictionary.Add("Ground Station", GlidernetAircraftType.Unknown);
        //_modelAircraftTypeDictionary.Add("OldTimer", GlidernetAircraftType.Unknown);
        _modelAircraftTypeDictionary.Add("Other", GlidernetAircraftType.Unknown);
        _modelAircraftTypeDictionary.Add("Unknown", GlidernetAircraftType.Unknown);
    }
}