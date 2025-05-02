namespace FlugsportWienOgn.Database.Entities;

public class FlightPathItem
{
    public int Id { get; set; }
    public int AircraftId { get; set; }
    public Aircraft Aircraft { get; set; }

    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public int Altitude { get; set; }
    public int Speed { get; set; }
    public float VerticalSpeed { get; set; }
    public DateTime Timestamp { get; set; }
    public string Receiver { get; set; }
}
