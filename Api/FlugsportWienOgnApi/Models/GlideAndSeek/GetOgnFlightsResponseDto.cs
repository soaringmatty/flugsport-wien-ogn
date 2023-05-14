namespace FlugsportWienOgnApi.Models.GlideAndSeek;

public class GetOgnFlightsResponseDto
{
    public string FlarmID { get; set; }
    public string DisplayName { get; set; }
    public string Registration { get; set; }
    public int Type { get; set; }
    public string Model { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public float Altitude { get; set; }
    public float Agl { get; set; }
    public long Timestamp { get; set; }
    public float Speed { get; set; }
    public float Vario { get; set; }
    public float VarioAverage { get; set; }
    public string Receiver { get; set; }
    public dynamic ReceiverPosition { get; set; }
}
