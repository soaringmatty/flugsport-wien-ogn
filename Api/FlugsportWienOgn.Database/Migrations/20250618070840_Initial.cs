using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FlugsportWienOgn.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Aircraft",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlarmId = table.Column<string>(type: "text", nullable: false),
                    Registration = table.Column<string>(type: "text", nullable: true),
                    CallSign = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    AircraftType = table.Column<int>(type: "integer", nullable: false),
                    Speed = table.Column<int>(type: "integer", nullable: false),
                    Altitude = table.Column<int>(type: "integer", nullable: false),
                    VerticalSpeed = table.Column<float>(type: "real", nullable: false),
                    VerticalSpeedAverage = table.Column<float>(type: "real", nullable: false),
                    Latitude = table.Column<float>(type: "real", nullable: false),
                    Longitude = table.Column<float>(type: "real", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRegistered = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aircraft", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cleanup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LastCleanup = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cleanup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnownAircraft",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlarmId = table.Column<string>(type: "text", nullable: false),
                    Registration = table.Column<string>(type: "text", nullable: false),
                    RegistrationShort = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    AircraftType = table.Column<int>(type: "integer", nullable: false),
                    OwnershipType = table.Column<int>(type: "integer", nullable: false),
                    Owner = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownAircraft", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlightbookEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AircraftId = table.Column<int>(type: "integer", nullable: false),
                    TakeOffTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LandingTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LaunchType = table.Column<int>(type: "integer", nullable: false),
                    LaunchHeight = table.Column<int>(type: "integer", nullable: true),
                    AirfieldIcao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightbookEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightbookEntry_Aircraft_AircraftId",
                        column: x => x.AircraftId,
                        principalTable: "Aircraft",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlightData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AircraftId = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<float>(type: "real", nullable: false),
                    Longitude = table.Column<float>(type: "real", nullable: false),
                    Altitude = table.Column<int>(type: "integer", nullable: false),
                    Speed = table.Column<int>(type: "integer", nullable: false),
                    VerticalSpeed = table.Column<float>(type: "real", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightData_Aircraft_AircraftId",
                        column: x => x.AircraftId,
                        principalTable: "Aircraft",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Cleanup",
                columns: new[] { "Id", "LastCleanup" },
                values: new object[] { 1, new DateOnly(2025, 6, 17) });

            migrationBuilder.InsertData(
                table: "KnownAircraft",
                columns: new[] { "Id", "AircraftType", "FlarmId", "Model", "Owner", "OwnershipType", "Registration", "RegistrationShort" },
                values: new object[,]
                {
                    { 1, 1, "DD98B4", "Ventus 2b", "ASKÖ Flugsport Wien", 0, "D-0544", "DR" },
                    { 2, 1, "DD9EA3", "LS4", "ASKÖ Flugsport Wien", 0, "D-2526", "DL" },
                    { 3, 1, "DDAD0C", "ASK 21", "ASKÖ Flugsport Wien", 0, "D-3931", "DZ" },
                    { 4, 1, "DD98C1", "LS4", "ASKÖ Flugsport Wien", 0, "D-9104", "DV" },
                    { 5, 1, "3F0625", "Discus‑2b", "ASKÖ Flugsport Wien", 0, "D-9614", "D2B" },
                    { 6, 1, "DD9537", "ASK 21", "ASKÖ Flugsport Wien", 0, "OE-5446", "DX" },
                    { 7, 1, "DDAF0B", "DG 300 Elan", "ASKÖ Flugsport Wien", 0, "OE-5491", "91" },
                    { 8, 1, "4404FD", "Ventus 2b", "ASKÖ Flugsport Wien", 0, "OE-5603", "DS" },
                    { 9, 1, "DD9F86", "DG 500 Orion", "ASKÖ Flugsport Wien", 0, "OE-5711", "DI" },
                    { 20, 3, "DD9382", "Dimona HK 36 TTC", "ASKÖ Flugsport Wien", 0, "D-KRES", "RES" },
                    { 21, 3, "440524", "Dimona HK 36 TTC", "ASKÖ Flugsport Wien", 0, "OE-9466", "466" },
                    { 22, 3, "440523", "Katana DA 20 A1", "ASKÖ Flugsport Wien", 0, "OE-CBB", "CBB" },
                    { 101, 1, "D0114B", "DG‑600", "Andreas Stocker", 1, "D-6000", "MI" },
                    { 102, 1, "D0287B", "LS1‑f", "Julia Götz", 1, "D-2254", "HR" },
                    { 103, 1, "D02864", "HPH 304S Shark", "Andreas Stocker / Julia Götz", 1, "D-KHJH", "JA" },
                    { 105, 1, "D0019F", "DG‑800", "Stephan Haupt", 1, "D-KEVA", "O2" },
                    { 106, 1, "F64550", "Ka‑8", "Christoph Urach", 1, "D-1890", "KA8" },
                    { 107, 1, "DF23C3", "Arcus M", "Markus Podivin / Irmgard Paul / Josef Pannagl", 1, "D-KWMR", "MR" },
                    { 108, 1, "3EEC8B", "HPH 304CZ‑17", "Sören Rossow", 1, "D-3060", "CZ" },
                    { 109, 1, "3EFBA7", "ASW 28", "Mario Neumann / Kathrin Havemann", 1, "D-6928", "SI" },
                    { 110, 1, "DDB2C4", "SF-27", "Fabian Hoffmann", 1, "OE-0789", "FH" },
                    { 111, 1, "D006D6", "EB 29 DR", "Christoph Jütte", 1, "D-KXAC", "AC" },
                    { 113, 1, "DD91B7", "DG-200", "Florian Wögerer", 1, "D-7868", "FLO" },
                    { 114, 1, "3EFBF6", "Mini Nimbus", "Ernst Schicker", 1, "D-7007", "SE" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aircraft_FlarmId",
                table: "Aircraft",
                column: "FlarmId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightbookEntry_AircraftId",
                table: "FlightbookEntry",
                column: "AircraftId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightData_AircraftId",
                table: "FlightData",
                column: "AircraftId");

            migrationBuilder.CreateIndex(
                name: "IX_KnownAircraft_FlarmId",
                table: "KnownAircraft",
                column: "FlarmId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cleanup");

            migrationBuilder.DropTable(
                name: "FlightbookEntry");

            migrationBuilder.DropTable(
                name: "FlightData");

            migrationBuilder.DropTable(
                name: "KnownAircraft");

            migrationBuilder.DropTable(
                name: "Aircraft");
        }
    }
}
