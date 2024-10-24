using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Додаємо нові поля до таблиці Events
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0); // Значення за замовчуванням для статусу

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0); // Значення за замовчуванням для категорії
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Видаляємо нові поля у випадку відкату міграції
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Events");
        }
    }
}
