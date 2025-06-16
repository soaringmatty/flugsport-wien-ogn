using FlugsportWienOgnApi.Models.Core;
using FlugsportWienOgnApi.Models.GlideAndSeek;

namespace FlugsportWienOgnApi.Utils;

public static class Mapping
{
    public static AircraftType MapAircraftType(GlideAndSeekAircraftType? rawType)
    {
        switch (rawType)
        {
            case GlideAndSeekAircraftType.Glider:
                return AircraftType.Glider;
            case GlideAndSeekAircraftType.Towplane:
                return AircraftType.Towplane;
            case GlideAndSeekAircraftType.Helicopter:
                return AircraftType.Helicopter;
            case GlideAndSeekAircraftType.Hangglider:
            case GlideAndSeekAircraftType.Paraglider:
                return AircraftType.HangOrParaglider;
            case GlideAndSeekAircraftType.Plane:
            case GlideAndSeekAircraftType.Jet:
                return AircraftType.Motorplane;
            default:
                return AircraftType.Unknown;
        }
    }
}
