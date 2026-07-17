using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSrsSchedulingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "lapse_count",
                table: "words",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_reviewed_at",
                table: "words",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "new_words_per_day",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddCheckConstraint(
                name: "chk_words_lapses",
                table: "words",
                sql: "lapse_count >= 0");

            migrationBuilder.CreateIndex(
                name: "idx_review_logs_word_time",
                table: "word_review_logs",
                columns: new[] { "word_id", "reviewed_at" });

            migrationBuilder.AddCheckConstraint(
                name: "chk_users_new_words",
                table: "users",
                sql: "new_words_per_day >= 0 AND new_words_per_day <= 100");

            // Backfill: last_reviewed_at distinguishes never-reviewed ("new") words from lapsed
            // ones. Take the latest review log; words reviewed before the log table existed
            // (repetitions > 0 but no log rows) fall back to created_at so they don't get
            // re-classified as new.
            migrationBuilder.Sql("""
                UPDATE words w SET last_reviewed_at =
                    (SELECT MAX(l.reviewed_at) FROM word_review_logs l WHERE l.word_id = w.id);

                UPDATE words SET last_reviewed_at = created_at
                WHERE last_reviewed_at IS NULL AND repetitions > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_words_lapses",
                table: "words");

            migrationBuilder.DropIndex(
                name: "idx_review_logs_word_time",
                table: "word_review_logs");

            migrationBuilder.DropCheckConstraint(
                name: "chk_users_new_words",
                table: "users");

            migrationBuilder.DropColumn(
                name: "lapse_count",
                table: "words");

            migrationBuilder.DropColumn(
                name: "last_reviewed_at",
                table: "words");

            migrationBuilder.DropColumn(
                name: "new_words_per_day",
                table: "users");
        }
    }
}
