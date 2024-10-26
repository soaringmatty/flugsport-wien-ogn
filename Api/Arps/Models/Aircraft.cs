namespace Arps.Models;

public record Aircraft(
    string Id,
    string? CallSign = null,
    string? Registration = null,
    string? Model = null,
    bool Visible = true,
    GlidernetAircraftType AircraftType = 0
)
{
    public override string ToString()
    {
        return !Visible
            ? "Aircraft: {{ <INVISIBLE> }}"
            : $"Aircraft: {{ ID: {Id}, call-sign: {CallSign}, registration: {Registration}, model: {Model} }}";
    }
}