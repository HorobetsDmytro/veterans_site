using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationCountInJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApplicationsCount",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationsCount",
                table: "Jobs");
        }
    }
}
