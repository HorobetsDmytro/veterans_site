using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultationBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBooking_AspNetUsers_UserId",
                table: "ConsultationBooking");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBooking_Consultations_ConsultationId",
                table: "ConsultationBooking");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConsultationBooking",
                table: "ConsultationBooking");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ConsultationBooking");

            migrationBuilder.RenameTable(
                name: "ConsultationBooking",
                newName: "ConsultationBookings");

            migrationBuilder.RenameIndex(
                name: "IX_ConsultationBooking_UserId",
                table: "ConsultationBookings",
                newName: "IX_ConsultationBookings_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ConsultationBooking_ConsultationId",
                table: "ConsultationBookings",
                newName: "IX_ConsultationBookings_ConsultationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConsultationBookings",
                table: "ConsultationBookings",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationBookings_AspNetUsers_UserId",
                table: "ConsultationBookings",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationBookings_Consultations_ConsultationId",
                table: "ConsultationBookings",
                column: "ConsultationId",
                principalTable: "Consultations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBookings_AspNetUsers_UserId",
                table: "ConsultationBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBookings_Consultations_ConsultationId",
                table: "ConsultationBookings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConsultationBookings",
                table: "ConsultationBookings");

            migrationBuilder.RenameTable(
                name: "ConsultationBookings",
                newName: "ConsultationBooking");

            migrationBuilder.RenameIndex(
                name: "IX_ConsultationBookings_UserId",
                table: "ConsultationBooking",
                newName: "IX_ConsultationBooking_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ConsultationBookings_ConsultationId",
                table: "ConsultationBooking",
                newName: "IX_ConsultationBooking_ConsultationId");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ConsultationBooking",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConsultationBooking",
                table: "ConsultationBooking",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationBooking_AspNetUsers_UserId",
                table: "ConsultationBooking",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationBooking_Consultations_ConsultationId",
                table: "ConsultationBooking",
                column: "ConsultationId",
                principalTable: "Consultations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
