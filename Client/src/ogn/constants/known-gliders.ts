import { Glider } from "../models/glider.model";

export const clubGliders: Glider[] = [
    {
        Owner: 'ASKÖ Flugsport Wien',
        Registration: 'D-0544',
        RegistrationShort: 'DR',
        Model: 'Ventus 2b',
        FlarmId: 'DD98B4'
    },
    {
        Owner: 'ASKÖ Flugsport Wien',
        Registration: 'D-2526',
        RegistrationShort: 'DL',
        Model: 'LS4',
        FlarmId: 'DD9EA3'
    },
    {
        Owner: 'ASKÖ Flugsport Wien',
        Registration: 'D-3931',
        RegistrationShort: 'DZ',
        Model: 'ASK 21',
        FlarmId: 'DDAD0C'
    },
    {
        Owner: 'ASKÖ Flugsport Wien',
        Registration: 'D-9104',
        RegistrationShort: 'DV',
        Model: 'LS4',
        FlarmId: 'DD98C1'
    },
    {
        Owner: 'ASKÖ Flugsport Wien',
        Registration: 'D-9614',
        RegistrationShort: 'D2B',
        Model: 'Discus-2b',
        FlarmId: '3F0625'
    },
    {
        Owner: 'ASKÖ Flugsport Wien',
        Registration: 'OE-5446',
        RegistrationShort: 'DX',
        Model: 'ASK 21',
        FlarmId: 'DD9537'
    },
    {
        Owner: 'ASKÖ Flugsport Wien',
        Registration: 'OE-5491',
        RegistrationShort: '91',
        Model: 'DG 300 Elan',
        FlarmId: 'DDAF0B'
    },
    {
        Owner: 'ASKÖ Flugsport Wien',
        Registration: 'OE-5603',
        RegistrationShort: 'DS',
        Model: 'Ventus 2b',
        FlarmId: '4404FD'
    },
    {
        Owner: 'ASKÖ Flugsport Wien',
        Registration: 'OE-5711',
        RegistrationShort: 'DI',
        Model: 'DG 500 Orion',
        FlarmId: 'DD9F86'
    },
    // {
    //   Owner: 'Test',
    //   Registration: 'D-TEST',
    //   RegistrationShort: 'TS2',
    //   Model: 'Club',
    //   FlarmId: '3EE265'
    // },
];

export const privateGliders: Glider[] = [
    {
        Owner: 'Fabian Hoffmann',
        Registration: 'D-KTJM',
        RegistrationShort: 'FH',
        Model: 'Valentin Kiwi',
        FlarmId: '3ECF12'
    },
    {
        Owner: 'Andreas Stocker',
        Registration: 'D-6000',
        RegistrationShort: 'MI',
        Model: 'DG-600',
        FlarmId: 'D0114B'
    },
    {
        Owner: 'Julia Götz',
        Registration: 'D-2254',
        RegistrationShort: 'HR',
        Model: 'LS1-f',
        FlarmId: 'DDFE83'
    },
    {
        Owner: 'Ernst Schicker',
        Registration: 'D-7007',
        RegistrationShort: 'SE',
        Model: 'Mini Nimbus',
        FlarmId: '3EFBF6'
    },
    {
        Owner: 'Stephan Haupt',
        Registration: 'D-KEVA',
        RegistrationShort: 'O2',
        Model: 'DG-800',
        FlarmId: 'D0019F'
    },
    {
        Owner: 'Thomas Dvorak',
        Registration: 'D-1648',
        RegistrationShort: 'DE',
        Model: 'ASW-20',
        FlarmId: '3EE707'
    },
    {
        Owner: 'Christoph Urach',
        Registration: 'D-1890',
        RegistrationShort: 'KA8',
        Model: 'Ka-8',
        FlarmId: 'F64550'
    },
    {
        Owner: 'Irmgard Paul',
        Registration: 'D-KXPP',
        RegistrationShort: 'PP',
        Model: 'ASH-26 E',
        FlarmId: 'DDB289'
    },
    {
        Owner: 'Sören Rossow',
        Registration: 'D-3060',
        RegistrationShort: 'CZ',
        Model: 'HPH 304CZ-17',
        FlarmId: '3EEC8B' // TODO (DF0F03)
    },
    // {
    //     Owner: 'Kathrin Havemann / Mario Neumann',
    //     Registration: 'D-6928',
    //     RegistrationShort: 'SI',
    //     Model: 'ASW 28',
    //     FlarmId: '?D-6928?' // TODO
    // },
    // {
    //   Owner: 'Test',
    //   Registration: 'D-TEST',
    //   RegistrationShort: 'TS2',
    //   Model: 'Private',
    //   FlarmId: 'DDAEF0'
    // },
];

export function getClubAndPrivateGliders(): Glider[] {
  return clubGliders.concat(privateGliders);
}
