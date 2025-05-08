using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSomeModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TaxiDrivers",
                keyColumn: "Id",
                keyValue: "driver1");

            migrationBuilder.DeleteData(
                table: "TaxiDrivers",
                keyColumn: "Id",
                keyValue: "driver2");

            migrationBuilder.DeleteData(
                table: "TaxiDrivers",
                keyColumn: "Id",
                keyValue: "driver3");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledTime",
                table: "TaxiRides",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "TaxiDrivers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CarModel",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledTime",
                table: "TaxiRides");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "TaxiDrivers");

            migrationBuilder.DropColumn(
                name: "CarModel",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CarNumber",
                table: "AspNetUsers");

            migrationBuilder.InsertData(
                table: "TaxiDrivers",
                columns: new[] { "Id", "CarModel", "CurrentLatitude", "CurrentLongitude", "IsAvailable", "LicensePlate", "Name", "PhoneNumber", "PhotoUrl", "Rating" },
                values: new object[,]
                {
                    { "driver1", "Toyota Camry", 50.450099999999999, 30.523399999999999, true, "АА1234ВВ", "Олександр Петренко", "+380991234567", "/images/drivers/driver1.jpg", 4.7999999999999998 },
                    { "driver2", "Hyundai Sonata", 50.451999999999998, 30.530000000000001, true, "АА5678ВС", "Сергій Коваленко", "+380992345678", "/images/drivers/driver2.jpg", 4.7000000000000002 },
                    { "driver3", "Volkswagen Passat", 50.447000000000003, 30.518000000000001, true, "АА9012ВТ", "Іван Мельник", "+380993456789", "/images/drivers/driver3.jpg", 4.9000000000000004 }
                });
        }
    }
}
