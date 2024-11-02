using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConsultationModel_5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingTime",
                table: "Consultations");

            migrationBuilder.AddColumn<int>(
                name: "SlotsCount",
                table: "Consultations",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlotsCount",
                table: "Consultations");

            migrationBuilder.AddColumn<DateTime>(
                name: "BookingTime",
                table: "Consultations",
                type: "datetime2",
                nullable: true);
        }
    }
}
