using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class MakePriceFieldNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualPrice",
                table: "TaxiRides");

            migrationBuilder.DropColumn(
                name: "EstimatedPrice",
                table: "TaxiRides");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ActualPrice",
                table: "TaxiRides",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EstimatedPrice",
                table: "TaxiRides",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
