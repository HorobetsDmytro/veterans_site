using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNewsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Author",
                table: "News",
                newName: "ImagePath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImagePath",
                table: "News",
                newName: "Author");
        }
    }
}
