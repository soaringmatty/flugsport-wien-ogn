namespace FlugsportWienOgnApi.Models.Core;

public class GliderListItem
{
    public string Owner { get; set; }
    public string Registration { get; set; }
    public string RegistrationShort { get; set; }
    public string Model { get; set; }
    public string FlarmId { get; set; }
    public GliderStatus Status { get; set; }
    public string Pilot { get; set; }
    public float DistanceFromHome { get; set; }
    public int Altitude { get; set; }
}

