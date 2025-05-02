namespace FlugsportWienOgn.Database.Entities;

public class Aircraft
{
    public int Id { get; set; }
    public string FlarmId { get; set; }
    public string? Registration { get; set; }
    public string? CallSign { get; set; }
    public string? Model { get; set; }
    public int AircraftType { get; set; }
    public int Speed { get; set; } // in km/h
    public int Altitude { get; set; } // in meters
    public float VerticalSpeed { get; set; } // in m/s
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime LastUpdate { get; set; }

    public List<FlightPathItem> FlightPath { get; set; } = new List<FlightPathItem>();
}

