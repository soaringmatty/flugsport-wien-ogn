namespace FlugsportWienOgn.Database.Entities;

public class KnownAircraft
{
    public int Id { get; set; }
    public required string FlarmId { get; set; }
    public required string Registration { get; set; }
    public required string RegistrationShort { get; set; }
    public required string Model { get; set; }
    public required int AircraftType { get; set; }
    public required int OwnershipType { get; set; }
    public string? Owner { get; set; }
}
