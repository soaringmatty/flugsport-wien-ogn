using FlugsportWienOgnApi.Models.Core;

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
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "OE-5491", RegistrationShort = "91", Model = "DG 300 Elan", FlarmId = "DD9663" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "OE-5603", RegistrationShort = "DS", Model = "Ventus 2b", FlarmId = "4404FD" },
        new Glider { Owner = "ASKÖ Flugsport Wien", Registration = "OE-5711", RegistrationShort = "DI", Model = "DG 500 Orion", FlarmId = "DD9F86" },
        //new Glider { Owner = "Test", Registration = "OE-TEST", RegistrationShort = "TMP", Model = "Test", FlarmId = "DD0C0B" }
    };

    public static List<Glider> PrivateGliders = new List<Glider>()
    {
        new Glider { Owner = "Fabian Hoffmann", Registration = "D-KTJM", RegistrationShort = "FH", Model = "Valentin Kiwi", FlarmId = "3ECF12" },
        new Glider { Owner = "Andreas Stocker", Registration = "D-6000", RegistrationShort = "MI", Model = "DG-600", FlarmId = "D0114B" },
        new Glider { Owner = "Julia Götz", Registration = "D-2254", RegistrationShort = "HR", Model = "LS-1 f", FlarmId = "DDFE83" },
        new Glider { Owner = "Ernst Schicker", Registration = "D-7007", RegistrationShort = "SE", Model = "Mini Nimbus", FlarmId = "3EFBF6" },
        new Glider { Owner = "Stephan Haupt", Registration = "D-KEVA", RegistrationShort = "O2", Model = "DG-800", FlarmId = "D0019F" },
        new Glider { Owner = "Kathrin Havemann / Mario Neumann", Registration = "D-6928", RegistrationShort = "SI", Model = "ASW 28", FlarmId = "?D-6928?" } // TODO
    };
}
