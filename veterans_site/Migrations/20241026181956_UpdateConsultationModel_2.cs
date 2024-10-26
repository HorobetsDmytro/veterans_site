using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConsultationModel_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultations_AspNetUsers_VeteranId",
                table: "Consultations");

            migrationBuilder.DropIndex(
                name: "IX_Consultations_VeteranId",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "VeteranId",
                table: "Consultations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VeteranId",
                table: "Consultations",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_VeteranId",
                table: "Consultations",
                column: "VeteranId");

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_AspNetUsers_VeteranId",
                table: "Consultations",
                column: "VeteranId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
