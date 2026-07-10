using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAiLogsTypeConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs");

            migrationBuilder.AddCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs",
                sql: "call_type IN ('format_words', 'generate_test', 'generate_fill_sentences', 'generate_distractors', 'suggest_title')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs");

            migrationBuilder.AddCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs",
                sql: "call_type IN ('format_words', 'generate_test', 'suggest_title')");
        }
    }
}
