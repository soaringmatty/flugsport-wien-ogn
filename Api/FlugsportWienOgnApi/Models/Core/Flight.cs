namespace FlugsportWienOgnApi.Models.Core;

public class Flight
{
    public string FlarmId { get; set; }
    public string DisplayName { get; set; }
    public string Registration { get; set; }
    public int Type { get; set; }
    public string Model { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public float HeightMSL { get; set; }
    public float HeightAGL { get; set; }
    public long Timestamp { get; set; }
    public float Speed { get; set; }
    public float Vario { get; set; }
    public float VarioAverage { get; set; }
    public string? Receiver { get; set; }
    public dynamic ReceiverPosition { get; set; }
}
