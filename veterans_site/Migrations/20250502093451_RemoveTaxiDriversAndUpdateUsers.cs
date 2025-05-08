using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaxiDriversAndUpdateUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxiRides_TaxiDrivers_DriverId",
                table: "TaxiRides");

            migrationBuilder.DropTable(
                name: "TaxiDrivers");

            migrationBuilder.RenameColumn(
                name: "CarNumber",
                table: "AspNetUsers",
                newName: "LicensePlate");

            migrationBuilder.AddColumn<double>(
                name: "CurrentLatitude",
                table: "AspNetUsers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLongitude",
                table: "AspNetUsers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "AspNetUsers",
                type: "float",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxiRides_AspNetUsers_DriverId",
                table: "TaxiRides",
                column: "DriverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxiRides_AspNetUsers_DriverId",
                table: "TaxiRides");

            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "LicensePlate",
                table: "AspNetUsers",
                newName: "CarNumber");

            migrationBuilder.CreateTable(
                name: "TaxiDrivers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CarModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentLatitude = table.Column<double>(type: "float", nullable: false),
                    CurrentLongitude = table.Column<double>(type: "float", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    LicensePlate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxiDrivers", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_TaxiRides_TaxiDrivers_DriverId",
                table: "TaxiRides",
                column: "DriverId",
                principalTable: "TaxiDrivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
