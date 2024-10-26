namespace FlugsportWienOgn.Database.Entities;

public class FlightPathItem
{
    public int Id { get; set; }
    public int PlaneId { get; set; }
    public Plane Plane { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Altitude { get; set; }
    public int Speed { get; set; }
    public float VerticalSpeed { get; set; }
    public DateTime Timestamp { get; set; }
    public string Receiver { get; set; }
}
