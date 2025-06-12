namespace Aprs.Models;

public enum AprsAircraftType
{
    Reserved = 0,
    GliderOrMotorGlider = 1,     // Glider, motor glider (turbo, jet, self-launch)
    TowPlane = 2,                // Tow plane / tug
    Helicopter = 3,              // Helicopter / gyrocopter / rotorcraft
    Skydiver = 4,                // Skydiver / parachute
    DropPlane = 5,               // Plane dropping skydivers
    HangGlider = 6,              // Hang glider (hard)
    Paraglider = 7,              // Paraglider (soft)
    PistonAircraft = 8,          // Reciprocating engine
    JetOrTurboprop = 9,          // Jet / turboprop engine
    Unknown = 10,                // Unknown
    Balloon = 11,                // Hot air, gas, weather, static
    Airship = 12,                // Airship, blimp, zeppelin
    UAV = 13,                    // Drone, UAV, RPAS
    Reserved2 = 14,
    StaticObstacle = 15          // Obstacle
}
