namespace FlugsportWienOgnApi.Models.Core;

public record Coordinate(
    double Latitude,
    double Longitude
)
{
    public override string ToString()
    {
        return $"Position: {{ latitude: {Latitude}, longitude: {Longitude} }}";
    }
}