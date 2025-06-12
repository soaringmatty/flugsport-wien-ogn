using FlugsportWienOgn.Database;
using FlugsportWienOgn.Database.Entities;
using FlugsportWienOgnApi.Models.Core;

namespace FlugsportWienOgnApi.Services;

public class KnownAircraftService
{
    public List<KnownAircraft> ClubGliders { get; }
    public HashSet<string> ClubGliderFlarmIds { get; }
    public HashSet<string> PrivateGliderFlarmIds { get; }
    public List<KnownAircraft> ClubAndPrivateGliders { get; }
    public List<KnownAircraft> ClubGlidersAndMotorplanes { get; }
    public HashSet<string> ClubAndPrivateGliderFlarmIds { get; }
    public HashSet<string> ClubGlidersAndMotorplaneFlarmIds { get; }
    public List<KnownAircraft> AllKnownPlanes { get; }
    public HashSet<string> AllKnownPlaneFlarmIds { get; }

    private List<KnownAircraft> ClubMotorPlanes { get; }
    private List<KnownAircraft> PrivateGliders { get; }

    public KnownAircraftService(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
        var knownGliders = dbContext.KnownAircraft.ToList();
        AllKnownPlanes = knownGliders.ToList();
        AllKnownPlaneFlarmIds = AllKnownPlanes.Select(x => x.FlarmId).ToHashSet();
        ClubGliders = knownGliders.Where(x => x.OwnershipType == (int)GliderOwnership.Club && x.AircraftType == (int)AircraftType.Glider).ToList();
        ClubGliderFlarmIds = ClubGliders.Select(x => x.FlarmId).ToHashSet();
        ClubMotorPlanes = knownGliders.Where(x => x.OwnershipType == (int)GliderOwnership.Club && x.AircraftType == (int)AircraftType.Motorplane).ToList();
        PrivateGliders = knownGliders.Where(x => x.OwnershipType == (int)GliderOwnership.Private).ToList();
        PrivateGliderFlarmIds = PrivateGliders.Select(x => x.FlarmId).ToHashSet();
        ClubAndPrivateGliders = ClubGliders.Concat(PrivateGliders).ToList();
        ClubAndPrivateGliderFlarmIds = ClubAndPrivateGliders.Select(x => x.FlarmId).ToHashSet();
        ClubGlidersAndMotorplanes = ClubGliders.Concat(ClubMotorPlanes).ToList();
        ClubGlidersAndMotorplaneFlarmIds = ClubGlidersAndMotorplanes.Select(x => x.FlarmId).ToHashSet();
    }

    public GliderOwnership GetGliderOwnershipByFlarmId(string flarmId)
    {
        if (ClubGlidersAndMotorplanes.Any(x => x.FlarmId == flarmId))
        {
            return GliderOwnership.Club;
        }
        if (PrivateGliders.Any(x => x.FlarmId == flarmId))
        {
            return GliderOwnership.Private;
        }
        return GliderOwnership.Foreign;
    }
}
