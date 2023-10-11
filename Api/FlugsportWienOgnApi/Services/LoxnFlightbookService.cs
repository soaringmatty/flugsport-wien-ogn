using Arps;
using FlugsportWienOgnApi.Models.Core;
using Microsoft.Extensions.Logging;

namespace FlugsportWienOgnApi.Services;

/// <summary>
/// TODO: If plane already flying when service starts -> don't count as takeoff
/// </summary>
public class LoxnFlightbookService
{
    //private const double LOXN_LATITUDE = 47.837841;
    //private const double LOXN_LONGITUDE = 16.220718;
    //private const int LOXN_ALTITUDE = 286; // in meters
    //private const int LOXN_RADIUS = 2; // in km

    private const double LOXN_LATITUDE = 51.141111;
    private const double LOXN_LONGITUDE = 6.986070;
    private const int LOXN_ALTITUDE = 92; // in meters
    private const int LOXN_RADIUS = 2; // in km

    private const int SPEED_THRESHOLD = 50; // in km/h
    private const int ALTITUDE_TOLERANCE = 20; // in meters

    private readonly ILogger<LoxnFlightbookService> _logger;
    private readonly LiveGliderService _liveGliderService;
    private readonly Dictionary<string, List<FlightPathItem>> _flightHistory;
    private readonly Dictionary<string, List<FlightEvent>> _flightEventHistory;

    public LoxnFlightbookService(ILogger<LoxnFlightbookService> logger)
    {
        _logger = logger;
        _flightHistory = new Dictionary<string, List<FlightPathItem>>();
        _flightEventHistory = new Dictionary<string, List<FlightEvent>>();

        _liveGliderService = new LiveGliderService(LOXN_LATITUDE, LOXN_LONGITUDE, LOXN_RADIUS);
        _liveGliderService.OnDataReceived += ProcessFlightDataUpdate;
        _liveGliderService.StartTracking();
    }

    private void ProcessFlightDataUpdate(FlightData flightData)
    {
        var isExisting = _flightHistory.TryGetValue(flightData.FlarmId, out var flightDataList);
        if (!isExisting)
        {
            flightDataList = new List<FlightPathItem>();
            _flightHistory.Add(flightData.FlarmId, flightDataList);
        }
        var flightPathItem = new FlightPathItem
        {
            Location = new Coordinate(flightData.Latitude, flightData.Longitude),
            Speed = Convert.ToInt32(flightData.Speed),
            Altitude = Convert.ToInt32(flightData.Altitude),
            VerticalSpeed = flightData.VerticalSpeed,
            Timestamp = flightData.DateTime
        };

        // Remove data older than 3 minutes
        var oldestTimestampToKeep = DateTime.Now.AddMinutes(-3);
        flightDataList.RemoveAll(p => p.Timestamp < oldestTimestampToKeep);

        // Add new entry to list
        flightDataList.Add(flightPathItem);

        // Check the plane's last event
        _flightEventHistory.TryGetValue(flightData.FlarmId, out var eventList);

        // If no event history for this plane exists, initialize it.
        if (eventList == null)
        {
            eventList = new List<FlightEvent>();
            _flightEventHistory[flightData.FlarmId] = eventList;
        }

        var lastEventEntry = eventList.LastOrDefault();
        var lastEvent = lastEventEntry?.FlightEventType ?? FlightEventType.Landing;
        var lastEventTime = lastEventEntry?.Timestamp ?? DateTime.MinValue;

        // Check for takeoff and landing events based on last event
        if (lastEvent == FlightEventType.Landing && HasTakenOff(flightPathItem))
        {
            if ((DateTime.Now - lastEventTime).TotalMinutes >= 2)
            {
                _logger.LogInformation($"{flightData.DateTime.ToShortTimeString()} {flightData.FlarmId}: Departure");
                AddFlightEvent(flightData.FlarmId, FlightEventType.Departure, flightPathItem.Timestamp);
            }
        }
        else if (lastEvent == FlightEventType.Departure && HasLanded(flightPathItem))
        {
            if ((DateTime.Now - lastEventTime).TotalMinutes >= 2)
            {
                _logger.LogInformation($"{flightData.DateTime.ToShortTimeString()} {flightData.FlarmId}: Landing");
                AddFlightEvent(flightData.FlarmId, FlightEventType.Landing, flightPathItem.Timestamp);
            }
        }

        // Check for winch launch
        if (lastEvent == FlightEventType.Departure && (DateTime.Now - lastEventTime).TotalSeconds >= 45 && !lastEventEntry.IsLaunchMethodChecked)
        {
            lastEventEntry.IsWinchLaunch = IsWinchLaunch(flightData.FlarmId, lastEventTime);
            lastEventEntry.IsLaunchMethodChecked = true;
            _logger.LogInformation($"{flightData.DateTime.ToShortTimeString()} {flightData.FlarmId}: Departure Type checked - Winch Launch: {lastEventEntry.IsWinchLaunch}");
        }
    }

    private void AddFlightEvent(string flarmId, FlightEventType eventType, DateTime timestamp)
    {
        var flightEvent = new FlightEvent
        {
            Timestamp = timestamp,
            FlightEventType = eventType
        };

        _flightEventHistory[flarmId].Add(flightEvent);
    }

    private bool HasTakenOff(FlightPathItem data)
    {
        return data.Speed > SPEED_THRESHOLD &&
               data.Altitude > LOXN_ALTITUDE + ALTITUDE_TOLERANCE;
    }

    private bool HasLanded(FlightPathItem data)
    {
        return data.Speed < SPEED_THRESHOLD &&
               data.Altitude < LOXN_ALTITUDE + ALTITUDE_TOLERANCE;
    }

    private bool IsWinchLaunch(string flarmId, DateTime departureTime)
    {
        if (!_flightHistory.TryGetValue(flarmId, out var flightPathItems))
            return false;

        var startItem = flightPathItems.FirstOrDefault(item => item.Timestamp == departureTime);
        if (startItem == null)
            return false;
        _logger.LogDebug($"startItemAltitude before: {startItem.Altitude}");

        double distanceToCheck = 500;
        double minAltitudeGainPerMeter = 0.4;

        double accumulatedDistance = 0;
        int altitudeGain = 0;

        var winchLaunchFlightPath = flightPathItems.SkipWhile(i => i != startItem).Skip(1);
        var lastUsedItem = startItem;
        foreach (var item in winchLaunchFlightPath)
        {
            accumulatedDistance += CalculateDistanceInMeters(lastUsedItem.Location, item.Location);
            altitudeGain = item.Altitude - startItem.Altitude;
            lastUsedItem = item;

            if (accumulatedDistance >= distanceToCheck)
            {
                break;
            }
                
        }

        var realAltitudeGainPerMeter = altitudeGain / accumulatedDistance;
        _logger.LogDebug($"alt/m: {realAltitudeGainPerMeter}, Gained Alt: {altitudeGain}, Distance: {accumulatedDistance}, startItemAltitude after: {startItem.Altitude}");
        return realAltitudeGainPerMeter >= minAltitudeGainPerMeter;
    }

    private double CalculateDistanceInMeters(Coordinate coord1, Coordinate coord2)
    {
        double dLat = (coord2.Latitude - coord1.Latitude) * Math.PI / 180.0;
        double dLon = (coord2.Longitude - coord1.Longitude) * Math.PI / 180.0;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(coord1.Latitude * Math.PI / 180.0) * Math.Cos(coord2.Latitude * Math.PI / 180.0) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return 6371.0 * c * 1000;
    }
}
