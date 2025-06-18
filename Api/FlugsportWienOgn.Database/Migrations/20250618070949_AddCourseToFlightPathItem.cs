using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlugsportWienOgn.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseToFlightPathItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Course",
                table: "FlightData",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Course",
                table: "FlightData");
        }
    }
}
