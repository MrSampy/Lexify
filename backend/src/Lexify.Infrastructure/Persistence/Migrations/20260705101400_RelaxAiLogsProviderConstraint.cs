using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RelaxAiLogsProviderConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_ai_logs_provider",
                table: "ai_call_logs");

            migrationBuilder.AddCheckConstraint(
                name: "chk_ai_logs_provider",
                table: "ai_call_logs",
                sql: "LENGTH(TRIM(provider)) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_ai_logs_provider",
                table: "ai_call_logs");

            migrationBuilder.AddCheckConstraint(
                name: "chk_ai_logs_provider",
                table: "ai_call_logs",
                sql: "provider IN ('ollama', 'openai')");
        }
    }
}
