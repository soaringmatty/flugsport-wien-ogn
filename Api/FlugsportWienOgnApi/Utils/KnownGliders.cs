﻿using FlugsportWienOgnApi.Models.Core;

namespace FlugsportWienOgnApi.Utils;

public static class KnownGliders
{
    public static List<Glider> ClubGliders = new List<Glider>()
    {
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "D-0544", RegistrationShort = "DR", Model = "Ventus 2b", FlarmId = "DD98B4" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "D-2526", RegistrationShort = "DL", Model = "LS4", FlarmId = "DD9EA3" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "D-3931", RegistrationShort = "DZ", Model = "ASK 21", FlarmId = "DDAD0C" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "D-9104", RegistrationShort = "DV", Model = "LS4", FlarmId = "DD98C1" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "D-9614", RegistrationShort = "D2B", Model = "Discus-2b", FlarmId = "3F0625" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "OE-5446", RegistrationShort = "DX", Model = "ASK 21", FlarmId = "DD9537" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "OE-5491", RegistrationShort = "91", Model = "DG 300 Elan", FlarmId = "DDAF0B" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "OE-5603", RegistrationShort = "DS", Model = "Ventus 2b", FlarmId = "4404FD" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "OE-5711", RegistrationShort = "DI", Model = "DG 500 Orion", FlarmId = "DD9F86" },
        //new Glider { Owner = "Test", Registration = "OE-TEST", RegistrationShort = "TST", Model = "Test", FlarmId = "3E7053" },
    };

    public static List<Glider> ClubMotorPlanes = new List<Glider>()
    {
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "D-KRES", RegistrationShort = "RES", Model = "Dimona HK 36 TTC", FlarmId = "3EC94E" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "OE-9466", RegistrationShort = "466", Model = "Dimona HK 36 TTC", FlarmId = "440524" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "OE-CBB", RegistrationShort = "CBB", Model = "Katana DA 20 A1", FlarmId = "440523" },
        //new Glider { Owner = "Test", Registration = "OE-TEST", RegistrationShort = "TST", Model = "Test", FlarmId = "4B26AD" },
    };

    public static List<Glider> PrivateGliders = new List<Glider>()
    {
        new Glider { Owner = "Fabian Hoffmann", Registration = "D-KTJM", RegistrationShort = "FH", Model = "Valentin Kiwi", FlarmId = "3ECF12" },
        new Glider { Owner = "Andreas Stocker", Registration = "D-6000", RegistrationShort = "MI", Model = "DG-600", FlarmId = "D0114B" },
        new Glider { Owner = "Julia Götz", Registration = "D-2254", RegistrationShort = "HR", Model = "LS1-f", FlarmId = "DDFE83" },
        new Glider { Owner = "Ernst Schicker", Registration = "D-7007", RegistrationShort = "SE", Model = "Mini Nimbus", FlarmId = "3EFBF6" },
        new Glider { Owner = "Stephan Haupt", Registration = "D-KEVA", RegistrationShort = "O2", Model = "DG-800", FlarmId = "D0019F" },
        new Glider { Owner = "Thomas Dvorak", Registration = "D-1648", RegistrationShort = "DE", Model = "ASW-20", FlarmId = "3EE707" },
        new Glider { Owner = "Christoph Urach", Registration = "D-1890", RegistrationShort = "KA8", Model = "Ka-8", FlarmId = "F64550" },
        new Glider { Owner = "Irmgard Paul", Registration = "D-KXPP", RegistrationShort = "PP", Model = "ASH-26 E", FlarmId = "DDB289" },
        new Glider { Owner = "Sören Rossow", Registration = "D-3060", RegistrationShort = "CZ", Model = "HPH 304CZ-17", FlarmId = "3EEC8B" }, // TODO (DF0F03)
        //new Glider { Owner = "Kathrin Havemann / Mario Neumann", Registration = "D-6928", RegistrationShort = "SI", Model = "ASW 28", FlarmId = "?D-6928?" }, // TODO
    };

    public static IEnumerable<Glider> ClubAndPrivateGliders = ClubGliders.Concat(PrivateGliders);

    public static IEnumerable<Glider> ClubGlidersAndMotorplanes = ClubGliders.Concat(ClubMotorPlanes);

    public static IEnumerable<Glider> AllKnownPlanes = ClubGliders.Concat(ClubMotorPlanes).Concat(PrivateGliders);

    public static GliderType GetGliderTypeByFlarmId(string flarmId)
    {
        if (ClubGlidersAndMotorplanes.Any(x => x.FlarmId == flarmId))
        {
            return GliderType.Club;
        }
        if (PrivateGliders.Any(x => x.FlarmId == flarmId))
        {
            return GliderType.Private;
        }
        return GliderType.Foreign;
    }
}
