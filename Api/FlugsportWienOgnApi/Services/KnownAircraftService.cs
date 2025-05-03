using FlugsportWienOgn.Database;
using FlugsportWienOgn.Database.Entities;
using FlugsportWienOgnApi.Models.Core;

namespace FlugsportWienOgnApi.Services;

public class KnownAircraftService
{
    public List<KnownAircraft> ClubGliders { get; }
    public List<KnownAircraft> ClubMotorPlanes { get; }
    public List<KnownAircraft> PrivateGliders { get; }
    public List<KnownAircraft> ClubAndPrivateGliders { get; }
    public List<KnownAircraft> ClubGlidersAndMotorplanes { get; }
    public List<KnownAircraft> AllKnownPlanes { get; }

    public KnownAircraftService(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
        var knownGliders = dbContext.KnownAircraft.ToList();
        AllKnownPlanes = knownGliders.ToList();
        ClubGliders = knownGliders.Where(x => x.OwnershipType == (int)GliderOwnership.Club && x.AircraftType == (int)AircraftType.Glider).ToList();
        ClubMotorPlanes = knownGliders.Where(x => x.OwnershipType == (int)GliderOwnership.Club && x.AircraftType == (int)AircraftType.Motorplane).ToList();
        PrivateGliders = knownGliders.Where(x => x.OwnershipType == (int)GliderOwnership.Private).ToList();
        ClubAndPrivateGliders = ClubGliders.Concat(PrivateGliders).ToList();
        ClubGlidersAndMotorplanes = ClubGliders.Concat(ClubMotorPlanes).ToList();
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
