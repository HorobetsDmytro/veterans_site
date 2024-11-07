using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace veterans_site.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Оновлюємо всі події з категорією Consultation на SocialEvent
            migrationBuilder.Sql(@"
            UPDATE Events 
            SET Category = 4 
            WHERE Category = 3");

            // Зсуваємо категорії після Consultation
            migrationBuilder.Sql(@"
            UPDATE Events 
            SET Category = Category - 1 
            WHERE Category > 3");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Повертаємо категорії назад
            migrationBuilder.Sql(@"
            UPDATE Events 
            SET Category = Category + 1 
            WHERE Category >= 3");

            migrationBuilder.Sql(@"
            UPDATE Events 
            SET Category = 3 
            WHERE Category = 4 
            AND EXISTS (SELECT 1 FROM Events WHERE Category = 4)");
        }
    }
}
