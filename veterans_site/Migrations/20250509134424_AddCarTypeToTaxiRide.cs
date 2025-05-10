using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class AddCarTypeToTaxiRide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CarTypes",
                table: "TaxiRides",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarTypes",
                table: "TaxiRides");
        }
    }
}
