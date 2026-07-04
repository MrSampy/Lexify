using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTestFailedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_tests_status",
                table: "tests");

            migrationBuilder.AddCheckConstraint(
                name: "chk_tests_status",
                table: "tests",
                sql: "status IN ('generating', 'ready', 'failed', 'archived')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_tests_status",
                table: "tests");

            migrationBuilder.AddCheckConstraint(
                name: "chk_tests_status",
                table: "tests",
                sql: "status IN ('generating', 'ready', 'archived')");
        }
    }
}
