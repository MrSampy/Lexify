using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailReminderPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "email_reminders_enabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_reminders_enabled",
                table: "users");
        }
    }
}
