using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "EventParticipants",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "ConsultationBookings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_ApplicationUserId",
                table: "EventParticipants",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationBookings_ApplicationUserId",
                table: "ConsultationBookings",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationBookings_AspNetUsers_ApplicationUserId",
                table: "ConsultationBookings",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventParticipants_AspNetUsers_ApplicationUserId",
                table: "EventParticipants",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBookings_AspNetUsers_ApplicationUserId",
                table: "ConsultationBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_EventParticipants_AspNetUsers_ApplicationUserId",
                table: "EventParticipants");

            migrationBuilder.DropIndex(
                name: "IX_EventParticipants_ApplicationUserId",
                table: "EventParticipants");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationBookings_ApplicationUserId",
                table: "ConsultationBookings");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "EventParticipants");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "ConsultationBookings");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegistrationDate",
                table: "AspNetUsers");
        }
    }
}
