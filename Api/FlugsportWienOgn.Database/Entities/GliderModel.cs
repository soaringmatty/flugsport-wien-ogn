namespace FlugsportWienOgn.Database.Entities;

public class GliderModel
{
    public int Id { get; set; }
    public required string Model { get; set; }
    public bool? SelfLaunch { get; set; }
}
