namespace FlugsportWienOgnApi.Models.Aprs;

public class FlightBookItem
{
    public string FlarmId { get; set; }
    public string Registration { get; set; }
    public string CallSign { get; set; }
    public string Model { get; set; }
    //public string Type { get; set; }
    public DateTime? TakeOffTimestamp { get; set; }
    public DateTime? LandingTimestamp { get; set; }
    public bool IsWinchLaunch { get; set; } = false;
    public bool IsLaunchMethodChecked { get; set; } = false;

    public override string ToString()
    {
        var takeOffTypeString = IsLaunchMethodChecked ? (IsWinchLaunch ? "Winde" : "Eigenstart / Fschlepp") : "unbekannt";
        return $"[{FlarmId}] {Registration} ({CallSign}) Typ: {Model} - Von: {TakeOffTimestamp?.ToShortTimeString()} Bis: {LandingTimestamp?.ToShortTimeString()} (Startart: {takeOffTypeString})";
    }
}
