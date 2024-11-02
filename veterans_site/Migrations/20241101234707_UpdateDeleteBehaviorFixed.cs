using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeleteBehaviorFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBookingRequests_ConsultationSlots_SlotId",
                table: "ConsultationBookingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBookingRequests_Consultations_ConsultationId",
                table: "ConsultationBookingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBookings_Consultations_ConsultationId",
                table: "ConsultationBookings");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationBookingRequests_ConsultationSlots_SlotId",
                table: "ConsultationBookingRequests",
                column: "SlotId",
                principalTable: "ConsultationSlots",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationBookingRequests_Consultations_ConsultationId",
                table: "ConsultationBookingRequests",
                column: "ConsultationId",
                principalTable: "Consultations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationBookings_Consultations_ConsultationId",
                table: "ConsultationBookings",
                column: "ConsultationId",
                principalTable: "Consultations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBookingRequests_ConsultationSlots_SlotId",
                table: "ConsultationBookingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBookingRequests_Consultations_ConsultationId",
                table: "ConsultationBookingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationBookings_Consultations_ConsultationId",
                table: "ConsultationBookings");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationBookingRequests_ConsultationSlots_SlotId",
                table: "ConsultationBookingRequests",
                column: "SlotId",
                principalTable: "ConsultationSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationBookingRequests_Consultations_ConsultationId",
                table: "ConsultationBookingRequests",
                column: "ConsultationId",
                principalTable: "Consultations",
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
    }
}
