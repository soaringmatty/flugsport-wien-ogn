namespace Arps;

public record FlightData(
    string FlarmId,
    float Speed,
    float Altitude,
    float VerticalSpeed,
    float TurnRate,
    float Course,
    float Latitude,
    float Longitude,
    DateTime DateTime
)
{
    public override string ToString()
    {
        return $"FlightData: {{ flarm-ID: {FlarmId}, altitude: {Altitude}, speed: {Speed}, vertical-speed: {VerticalSpeed}, turn-rate: {TurnRate}, course: {Course}, datetime: {DateTime}, latitude: {Latitude}, longitude: {Longitude} }}";
    }
}