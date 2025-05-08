using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialTaxiTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxiDrivers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CarModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LicensePlate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    CurrentLatitude = table.Column<double>(type: "float", nullable: false),
                    CurrentLongitude = table.Column<double>(type: "float", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxiDrivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxiRides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VeteranId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EndAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartLatitude = table.Column<double>(type: "float", nullable: false),
                    StartLongitude = table.Column<double>(type: "float", nullable: false),
                    EndLatitude = table.Column<double>(type: "float", nullable: false),
                    EndLongitude = table.Column<double>(type: "float", nullable: false),
                    EstimatedPrice = table.Column<double>(type: "float", nullable: false),
                    ActualPrice = table.Column<double>(type: "float", nullable: false),
                    RequestTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PickupTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompleteTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxiRides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxiRides_AspNetUsers_VeteranId",
                        column: x => x.VeteranId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxiRides_TaxiDrivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "TaxiDrivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TaxiDrivers",
                columns: new[] { "Id", "CarModel", "CurrentLatitude", "CurrentLongitude", "IsAvailable", "LicensePlate", "Name", "PhoneNumber", "PhotoUrl", "Rating" },
                values: new object[,]
                {
                    { "driver1", "Toyota Camry", 50.450099999999999, 30.523399999999999, true, "АА1234ВВ", "Олександр Петренко", "+380991234567", "/images/drivers/driver1.jpg", 4.7999999999999998 },
                    { "driver2", "Hyundai Sonata", 50.451999999999998, 30.530000000000001, true, "АА5678ВС", "Сергій Коваленко", "+380992345678", "/images/drivers/driver2.jpg", 4.7000000000000002 },
                    { "driver3", "Volkswagen Passat", 50.447000000000003, 30.518000000000001, true, "АА9012ВТ", "Іван Мельник", "+380993456789", "/images/drivers/driver3.jpg", 4.9000000000000004 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxiRides_DriverId",
                table: "TaxiRides",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxiRides_VeteranId",
                table: "TaxiRides",
                column: "VeteranId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxiRides");

            migrationBuilder.DropTable(
                name: "TaxiDrivers");
        }
    }
}
