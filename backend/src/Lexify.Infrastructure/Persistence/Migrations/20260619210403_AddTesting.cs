using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTesting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "generating"),
                    question_count = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tests", x => x.id);
                    table.CheckConstraint("chk_tests_status", "status IN ('generating', 'ready', 'archived')");
                    table.ForeignKey(
                        name: "fk_tests_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: true),
                    question_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    question_text = table.Column<string>(type: "text", nullable: false),
                    correct_answer = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.id);
                    table.CheckConstraint("chk_questions_type", "question_type IN ('translate_to_native', 'translate_to_foreign', 'fill_in_sentence', 'multi_select_theme', 'open_answer')");
                    table.ForeignKey(
                        name: "fk_questions_test",
                        column: x => x.test_id,
                        principalTable: "tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_questions_word",
                        column: x => x.word_id,
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "test_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    score = table.Column<double>(type: "double precision", nullable: true),
                    total_questions = table.Column<int>(type: "integer", nullable: true),
                    correct_answers = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_attempts", x => x.id);
                    table.CheckConstraint("chk_attempts_counts", "(total_questions IS NULL AND correct_answers IS NULL) OR (total_questions >= 0 AND correct_answers >= 0 AND correct_answers <= total_questions)");
                    table.CheckConstraint("chk_attempts_score", "score IS NULL OR (score >= 0.0 AND score <= 1.0)");
                    table.ForeignKey(
                        name: "fk_attempts_test",
                        column: x => x.test_id,
                        principalTable: "tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_attempts_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_blocks",
                columns: table => new
                {
                    test_id = table.Column<Guid>(type: "uuid", nullable: false),
                    block_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_blocks", x => new { x.test_id, x.block_id });
                    table.ForeignKey(
                        name: "fk_test_blocks_block",
                        column: x => x.block_id,
                        principalTable: "word_blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_test_blocks_test",
                        column: x => x.test_id,
                        principalTable: "tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_text = table.Column<string>(type: "text", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_question_options_question",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attempt_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    given_answer = table.Column<string>(type: "text", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    time_spent_ms = table.Column<int>(type: "integer", nullable: true),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attempt_answers", x => x.id);
                    table.CheckConstraint("chk_answers_time", "time_spent_ms IS NULL OR time_spent_ms >= 0");
                    table.ForeignKey(
                        name: "fk_answers_attempt",
                        column: x => x.attempt_id,
                        principalTable: "test_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_answers_question",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_attempt_answers_question",
                table: "attempt_answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "idx_attempt_answers_wrong",
                table: "attempt_answers",
                column: "attempt_id",
                filter: "is_correct = FALSE");

            migrationBuilder.CreateIndex(
                name: "uq_attempt_question",
                table: "attempt_answers",
                columns: new[] { "attempt_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_question_options_correct",
                table: "question_options",
                column: "question_id",
                filter: "is_correct = TRUE");

            migrationBuilder.CreateIndex(
                name: "idx_questions_hash",
                table: "questions",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "idx_questions_sort",
                table: "questions",
                columns: new[] { "test_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "idx_questions_test_id",
                table: "questions",
                column: "test_id");

            migrationBuilder.CreateIndex(
                name: "idx_questions_word_id",
                table: "questions",
                column: "word_id",
                filter: "word_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_attempts_incomplete",
                table: "test_attempts",
                column: "user_id",
                filter: "finished_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_attempts_started",
                table: "test_attempts",
                columns: new[] { "user_id", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_attempts_test_id",
                table: "test_attempts",
                column: "test_id");

            migrationBuilder.CreateIndex(
                name: "idx_test_blocks_block",
                table: "test_blocks",
                column: "block_id");

            migrationBuilder.CreateIndex(
                name: "idx_tests_created",
                table: "tests",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_tests_status",
                table: "tests",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_tests_user_id",
                table: "tests",
                column: "user_id");

            // EF Core merges HasIndex calls on the same column(s) — add plain indexes via raw SQL
            migrationBuilder.Sql("CREATE INDEX idx_question_options_question ON question_options (question_id);");
            migrationBuilder.Sql("CREATE INDEX idx_attempts_user_id ON test_attempts (user_id);");
            migrationBuilder.Sql("CREATE INDEX idx_attempt_answers_attempt ON attempt_answers (attempt_id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_attempt_answers_attempt;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_attempts_user_id;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_question_options_question;");

            migrationBuilder.DropTable(
                name: "attempt_answers");

            migrationBuilder.DropTable(
                name: "question_options");

            migrationBuilder.DropTable(
                name: "test_blocks");

            migrationBuilder.DropTable(
                name: "test_attempts");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "tests");
        }
    }
}
