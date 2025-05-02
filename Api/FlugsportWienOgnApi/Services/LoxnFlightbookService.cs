using Aprs;
using Aprs.Models;
using FlugsportWienOgnApi.Models.Aprs;
using FlugsportWienOgnApi.Models.Core;

namespace FlugsportWienOgnApi.Services;

[Obsolete]
public class LoxnFlightbookService
{
    // LOXN
    //private const double AIRPORT_LATITUDE = 47.837841;
    //private const double AIRPORT_LONGITUDE = 16.220718;
    //private const int AIRPORT_ALTITUDE = 286; // in meters
    //private const int AIRPORT_RADIUS = 2; // in km

    // TEST - Unterwössen
    private const double AIRPORT_LATITUDE = 45.485717;
    private const double AIRPORT_LONGITUDE = -75.096547;
    private const int AIRPORT_ALTITUDE = 75; // in meters
    private const int AIRPORT_RADIUS = 2; // in km

    // OnGround / InAir distinctive features
    private const int FLYING_SPEED_THRESHOLD = 50; // in km/h
    private const int FLYING_ALTITUDE_TOLERANCE = 25; // in meters
    private const int FLYING_SAME_EVENT_TIMEOUT = 180; // in seconds
    private const int AFTER_TAKEOFF_EVENT_TIMEOUT = 15; // in seconds
    // Memory
    private const int MAX_FLIGHT_DATA_AGE = 3; // in minutes
    // IsWinchLaunch Parameters
    private const int WINCH_LAUNCH_CHECK_TIMEOUT = 30; // in seconds
    private const int WINCH_LAUNCH_DISTANCE = 500; // in meters
    private const double WINCH_LAUNCH_MIN_HEIGHT_GAIN = 0.4; // in vertical meters per horizontal meter

    private readonly ILogger<LoxnFlightbookService> _logger;
    private readonly AircraftProvider _aircraftProvider;
    private readonly LiveGliderService _liveGliderService;
    private readonly Dictionary<string, LiveFlight> _flightHistory;
    private readonly List<FlightBookItem> _flightBook;

    public List<FlightBookItem> FlightBook { get => _flightBook; }

    public LoxnFlightbookService(ILogger<LoxnFlightbookService> logger, AircraftProvider aircraftProvider, LiveGliderService liveGliderService)
    {
        _logger = logger;
        _flightHistory = new Dictionary<string, LiveFlight>();
        _flightBook = new List<FlightBookItem>();
        _aircraftProvider = aircraftProvider;
        _liveGliderService = liveGliderService;
        _liveGliderService.FlightDataReceived += ProcessFlightDataUpdate;
        Task.Factory.StartNew(async () =>
        {
            await _aircraftProvider.InitializeAsync(CancellationToken.None);
            //await _liveGliderService.StartTracking();
        });
    }

    private void ProcessFlightDataUpdate(FlightData flightData)
    {
        // Add flight data to dictionary
        var flightPathItem = new FlightPathItem
        {
            Location = new Coordinate(flightData.Latitude, flightData.Longitude),
            Speed = Convert.ToInt32(flightData.Speed),
            Altitude = Convert.ToInt32(flightData.Altitude),
            VerticalSpeed = flightData.VerticalSpeed,
            Timestamp = flightData.Time
        };
        var isFirstEntry = !_flightHistory.TryGetValue(flightData.FlarmId, out var liveFlight);
        if (isFirstEntry)
        {
            liveFlight = new LiveFlight();
            _flightHistory.Add(flightData.FlarmId, liveFlight);
        }
        liveFlight.FlightPath.Add(flightPathItem);

        // Check the plane's last event
        var lastFlightBookEntry = _flightBook.LastOrDefault(x => x.FlarmId == flightData.FlarmId);
        var lastEventType = lastFlightBookEntry != null && !lastFlightBookEntry.LandingTimestamp.HasValue ? FlightEventType.TakeOff : FlightEventType.Landing;
        var lastEventTime = liveFlight.FlightStatusChangedTime ?? DateTime.MinValue;

        var lastFlightStatus = liveFlight.FlightStatus;
        var isFlying = IsFlying(flightPathItem);
        var elapsedSecondsSinceLastEvent = liveFlight.FlightStatusChangedTime.HasValue ? (DateTime.Now - lastEventTime).TotalSeconds : int.MaxValue;

        // If first entry -> Skip checking for takeoff/landing event and check current flight state
        // Else, check for takeoff and landing events
        if (!isFirstEntry && isFlying && lastFlightStatus == FlightStatus.OnGround)
        {
            AddFlightEvent(flightData.FlarmId, FlightEventType.TakeOff, flightPathItem.Timestamp);
            liveFlight.FlightStatusChangedTime = flightPathItem.Timestamp;
        }
        else if (!isFirstEntry && !isFlying && lastFlightStatus == FlightStatus.Flying && elapsedSecondsSinceLastEvent > AFTER_TAKEOFF_EVENT_TIMEOUT)
        {
            AddFlightEvent(flightData.FlarmId, FlightEventType.Landing, flightPathItem.Timestamp);
            liveFlight.FlightStatusChangedTime = flightPathItem.Timestamp;
        }
        liveFlight.FlightStatus = isFlying ? FlightStatus.Flying : FlightStatus.OnGround;

        // Check for winch launch
        if (lastFlightBookEntry != null && lastEventType == FlightEventType.TakeOff && elapsedSecondsSinceLastEvent >= WINCH_LAUNCH_CHECK_TIMEOUT && !lastFlightBookEntry.IsLaunchMethodChecked)
        {
            lastFlightBookEntry.IsWinchLaunch = IsWinchLaunch(flightData.FlarmId, lastEventTime);
            lastFlightBookEntry.IsLaunchMethodChecked = true;
            _logger.LogInformation($"{flightData.Time.ToShortTimeString()} {flightData.FlarmId}: Departure Type checked - Winch Launch: {lastFlightBookEntry.IsWinchLaunch}");
        }

        // Remove data older than the max allowed flight data age
        var oldestTimestampToKeep = DateTime.Now.AddMinutes(MAX_FLIGHT_DATA_AGE * -1);
        liveFlight.FlightPath.RemoveAll(p => p.Timestamp < oldestTimestampToKeep);
    }

    private void AddFlightEvent(string flarmId, FlightEventType eventType, DateTime timestamp)
    {
        var aircraftData = _aircraftProvider.Load(flarmId);
        var flightBookEntry = new FlightBookItem
        {
            FlarmId = flarmId,
            Registration = aircraftData?.Registration,
            CallSign = aircraftData?.CallSign,
            Model = aircraftData?.Model,
        };
        if (eventType == FlightEventType.TakeOff)
        {
            flightBookEntry.TakeOffTimestamp = timestamp;
            _flightBook.Add(flightBookEntry);
        }
        else if (eventType == FlightEventType.Landing)
        {
            var existingEntry = _flightBook.LastOrDefault(x => x.FlarmId == flarmId && !x.LandingTimestamp.HasValue);
            if (existingEntry == null)
            {
                _flightBook.Add(flightBookEntry);
            }
            else
            {
                flightBookEntry = existingEntry;
            }
            flightBookEntry.LandingTimestamp = timestamp;
        }
        _logger.LogInformation(flightBookEntry.ToString());
    }

    private bool IsFlying(FlightPathItem data)
    {
        return data.Speed >= FLYING_SPEED_THRESHOLD &&
               data.Altitude >= AIRPORT_ALTITUDE + FLYING_ALTITUDE_TOLERANCE;
    }

    private bool IsWinchLaunch(string flarmId, DateTime departureTime)
    {
        if (!_flightHistory.TryGetValue(flarmId, out var liveFlight))
            return false;

        var startItem = liveFlight.FlightPath.FirstOrDefault(item => item.Timestamp == departureTime);
        if (startItem == null)
            return false;

        double accumulatedDistance = 0;
        int altitudeGain = 0;

        var winchLaunchFlightPath = liveFlight.FlightPath.SkipWhile(i => i != startItem).Skip(1);
        var lastUsedItem = startItem;
        foreach (var item in winchLaunchFlightPath)
        {
            accumulatedDistance += CalculateDistanceInMeters(lastUsedItem.Location, item.Location);
            altitudeGain = item.Altitude - startItem.Altitude;
            lastUsedItem = item;
            if (accumulatedDistance >= WINCH_LAUNCH_DISTANCE)
            {
                break;
            }   
        }
        var realAltitudeGainPerMeter = altitudeGain / accumulatedDistance;
        _logger.LogDebug($"alt/m: {realAltitudeGainPerMeter}, Gained Alt: {altitudeGain}, Distance: {accumulatedDistance}");
        return realAltitudeGainPerMeter >= WINCH_LAUNCH_MIN_HEIGHT_GAIN;
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
