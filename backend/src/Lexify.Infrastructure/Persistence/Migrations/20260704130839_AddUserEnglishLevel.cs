using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEnglishLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "english_level",
                table: "users",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "chk_users_english_level",
                table: "users",
                sql: "english_level IS NULL OR english_level IN ('A1', 'A2', 'B1', 'B2', 'C1', 'C2')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_users_english_level",
                table: "users");

            migrationBuilder.DropColumn(
                name: "english_level",
                table: "users");
        }
    }
}
