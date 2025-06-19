namespace FlugsportWienOgnApi.Models.Core;

public class GliderListFilter
{
    public bool KnownGlidersOnly { get; set; } = false;
    public bool IncludeLaunchTypeWinch { get; set; } = true;
    public bool IncludeLaunchTypeAerotow { get; set; } = true;
    public bool IncludeLaunchTypeMotorized { get; set; } = true;
    public bool IncludeLaunchTypeUnknown { get; set; } = true;
    public string? FlarmId { get; set; }
    public string? TowPlaneFlarmId { get; set; }
}
