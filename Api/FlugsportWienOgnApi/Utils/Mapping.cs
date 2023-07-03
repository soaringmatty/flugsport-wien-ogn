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
            flights.Add(new Flight
            {
                FlarmId = rawFlight.FlarmID,
                DisplayName = rawFlight.DisplayName,
                Registration = rawFlight.Registration,
                Type = rawFlight.Type,
                Model = rawFlight.Model,
                Latitude = rawFlight.Lat,
                Longitude = rawFlight.Lng,
                HeightMSL = rawFlight.Altitude,
                HeightAGL = rawFlight.Agl,
                Timestamp = rawFlight.Timestamp,
                Speed = rawFlight.Speed,
                Vario = rawFlight.Vario,
                VarioAverage = rawFlight.VarioAverage,
                Receiver = rawFlight.Receiver,
                ReceiverPosition = rawFlight.ReceiverPosition
            });
        }
        return flights;
    }
}
