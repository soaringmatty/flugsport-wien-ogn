using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlugsportWienOgn.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTowFlightEntryIdToFlightbookEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TowFlightEntryId",
                table: "FlightbookEntry",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightbookEntry_TowFlightEntryId",
                table: "FlightbookEntry",
                column: "TowFlightEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlightbookEntry_FlightbookEntry_TowFlightEntryId",
                table: "FlightbookEntry",
                column: "TowFlightEntryId",
                principalTable: "FlightbookEntry",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlightbookEntry_FlightbookEntry_TowFlightEntryId",
                table: "FlightbookEntry");

            migrationBuilder.DropIndex(
                name: "IX_FlightbookEntry_TowFlightEntryId",
                table: "FlightbookEntry");

            migrationBuilder.DropColumn(
                name: "TowFlightEntryId",
                table: "FlightbookEntry");
        }
    }
}
