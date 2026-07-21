using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFiveQuestionTypesAndWordDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_questions_type",
                table: "questions");

            migrationBuilder.DropCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs");

            migrationBuilder.AddColumn<string>(
                name: "definition",
                table: "words",
                type: "text",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "chk_questions_type",
                table: "questions",
                sql: "question_type IN ('translate_to_native', 'translate_to_foreign', 'fill_in_sentence', 'multi_select_theme', 'open_answer', 'matching_pairs', 'listen_and_type', 'word_scramble', 'sentence_builder', 'definition_match')");

            migrationBuilder.AddCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs",
                sql: "call_type IN ('format_words', 'generate_test', 'generate_fill_sentences', 'generate_distractors', 'generate_definitions', 'suggest_title')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_questions_type",
                table: "questions");

            migrationBuilder.DropCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs");

            migrationBuilder.DropColumn(
                name: "definition",
                table: "words");

            migrationBuilder.AddCheckConstraint(
                name: "chk_questions_type",
                table: "questions",
                sql: "question_type IN ('translate_to_native', 'translate_to_foreign', 'fill_in_sentence', 'multi_select_theme', 'open_answer')");

            migrationBuilder.AddCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs",
                sql: "call_type IN ('format_words', 'generate_test', 'generate_fill_sentences', 'generate_distractors', 'suggest_title')");
        }
    }
}
