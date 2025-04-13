using FlugsportWienOgnApi.Models.Core;
using FlugsportWienOgnApi.Models.GlideAndSeek;

namespace FlugsportWienOgnApi.Utils;

public static class Mapping
{
    public static IEnumerable<Flight> MapOgnFlightsResponseToFlights(IEnumerable<GetOgnFlightsResponseDto> rawFlights)
    {
        List<Flight> flights = new();
        foreach (var rawFlight in rawFlights)
        {
            var gliderType = KnownGliders.GetGliderTypeByFlarmId(rawFlight.FlarmID);
            var aircraftType = MapAircraftType(rawFlight.Type);
            var displayName = rawFlight.DisplayName;
            if (gliderType == GliderType.Club)
            {
                var glider = KnownGliders.ClubGlidersAndMotorplanes.FirstOrDefault(x => x.FlarmId == rawFlight.FlarmID);
                displayName = glider?.RegistrationShort;
            }
            flights.Add(new Flight
            {
                FlarmId = rawFlight.FlarmID,
                DisplayName = displayName,
                Registration = rawFlight.Registration,
                Type = gliderType,
                AircraftType = aircraftType,
                Model = rawFlight.Model,
                Latitude = rawFlight.Lat,
                Longitude = rawFlight.Lng,
                HeightMSL = rawFlight.Altitude,
                HeightAGL = rawFlight.Agl,
                Timestamp = rawFlight.Timestamp,
                Speed = rawFlight.Speed,
                Vario = rawFlight.Vario,
                VarioAverage = rawFlight.VarioAverage
            });
        }
        return flights;
    }
    public static AircraftType MapAircraftType(GlideAndSeekAircraftType? rawType)
    {
        switch (rawType)
        {
            case GlideAndSeekAircraftType.Glider:
                return AircraftType.Glider;
            case GlideAndSeekAircraftType.Towplane:
                return AircraftType.Towplane;
            case GlideAndSeekAircraftType.Helicopter:
                return AircraftType.Helicopter;
            case GlideAndSeekAircraftType.Hangglider:
            case GlideAndSeekAircraftType.Paraglider:
                return AircraftType.HangOrParaglider;
            case GlideAndSeekAircraftType.Plane:
            case GlideAndSeekAircraftType.Jet:
                return AircraftType.Motorplane;
            default:
                return AircraftType.Unknown;
        }
    }
}
