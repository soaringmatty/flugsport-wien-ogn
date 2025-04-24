namespace Aprs.Models;

public record FlightData(
    string FlarmId,
    float Speed,
    float Altitude,
    float VerticalSpeed,
    float TurnRate,
    float Course,
    float Latitude,
    float Longitude,
    DateTime ReceiverTimeStamp,
    DateTime Time,
    string Receiver
)
{
    public override string ToString()
    {
        return $"FlightData: {{ flarm-ID: {FlarmId}, altitude: {Altitude}, speed: {Speed}, vertical-speed: {VerticalSpeed}, turn-rate: {TurnRate}, course: {Course}, datetime: {Time}, latitude: {Latitude}, longitude: {Longitude} }}";
    }
}