using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConsultationModel_3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Спочатку додаємо нові колонки без обмежень
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Consultations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BookingTime",
                table: "Consultations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBooked",
                table: "Consultations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Оновлюємо існуючі записи, встановлюючи UserId в null
            migrationBuilder.Sql(@"
            UPDATE Consultations
            SET UserId = NULL,
                IsBooked = 0,
                BookingTime = NULL");

            // Тепер додаємо індекс та зовнішній ключ
            migrationBuilder.CreateIndex(
                name: "IX_Consultations_UserId",
                table: "Consultations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_AspNetUsers_UserId",
                table: "Consultations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultations_AspNetUsers_UserId",
                table: "Consultations");

            migrationBuilder.DropIndex(
                name: "IX_Consultations_UserId",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "BookingTime",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "IsBooked",
                table: "Consultations");
        }
    }
}
