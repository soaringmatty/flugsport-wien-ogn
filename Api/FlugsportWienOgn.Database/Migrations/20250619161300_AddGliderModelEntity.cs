using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FlugsportWienOgn.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGliderModelEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlightbookEntry_FlightbookEntry_TowFlightEntryId",
                table: "FlightbookEntry");

            migrationBuilder.DropTable(
                name: "Cleanup");

            migrationBuilder.CreateTable(
                name: "GliderModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Model = table.Column<string>(type: "text", nullable: false),
                    SelfLaunch = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GliderModel", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GliderModel_Model",
                table: "GliderModel",
                column: "Model",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FlightbookEntry_FlightbookEntry_TowFlightEntryId",
                table: "FlightbookEntry",
                column: "TowFlightEntryId",
                principalTable: "FlightbookEntry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlightbookEntry_FlightbookEntry_TowFlightEntryId",
                table: "FlightbookEntry");

            migrationBuilder.DropTable(
                name: "GliderModel");

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

            migrationBuilder.InsertData(
                table: "Cleanup",
                columns: new[] { "Id", "LastCleanup" },
                values: new object[] { 1, new DateOnly(2025, 6, 17) });

            migrationBuilder.AddForeignKey(
                name: "FK_FlightbookEntry_FlightbookEntry_TowFlightEntryId",
                table: "FlightbookEntry",
                column: "TowFlightEntryId",
                principalTable: "FlightbookEntry",
                principalColumn: "Id");
        }
    }
}
