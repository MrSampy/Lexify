using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "word_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<short>(type: "smallint", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    word_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_word_blocks", x => x.id);
                    table.CheckConstraint("chk_word_blocks_count", "word_count >= 0");
                    table.CheckConstraint("chk_word_blocks_title", "LENGTH(TRIM(title)) > 0");
                    table.ForeignKey(
                        name: "fk_word_blocks_language",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_word_blocks_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "block_tags",
                columns: table => new
                {
                    block_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_block_tags", x => new { x.block_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_block_tags_block",
                        column: x => x.block_id,
                        principalTable: "word_blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_block_tags_tag",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "words",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    block_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term = table.Column<string>(type: "text", nullable: false),
                    translation = table.Column<string>(type: "text", nullable: false),
                    word_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "word"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    example_sentence = table.Column<string>(type: "text", nullable: true),
                    confidence_flag = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    confidence_note = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ease_factor = table.Column<double>(type: "double precision", nullable: false, defaultValue: 2.5),
                    interval_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    repetitions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_review_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_words", x => x.id);
                    table.CheckConstraint("chk_words_ease", "ease_factor >= 1.3");
                    table.CheckConstraint("chk_words_interval", "interval_days >= 1");
                    table.CheckConstraint("chk_words_reps", "repetitions >= 0");
                    table.CheckConstraint("chk_words_term", "LENGTH(TRIM(term)) > 0");
                    table.CheckConstraint("chk_words_trans", "LENGTH(TRIM(translation)) > 0");
                    table.CheckConstraint("chk_words_type", "word_type IN ('word', 'phrase', 'idiom', 'expression')");
                    table.ForeignKey(
                        name: "fk_words_block",
                        column: x => x.block_id,
                        principalTable: "word_blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_block_tags_tag",
                table: "block_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "idx_word_blocks_language",
                table: "word_blocks",
                columns: new[] { "user_id", "language_id" });

            migrationBuilder.CreateIndex(
                name: "idx_word_blocks_updated",
                table: "word_blocks",
                columns: new[] { "user_id", "updated_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_word_blocks_user_id",
                table: "word_blocks",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_word_blocks_language_id",
                table: "word_blocks",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "idx_words_confidence",
                table: "words",
                column: "block_id",
                filter: "confidence_flag = TRUE");

            migrationBuilder.CreateIndex(
                name: "idx_words_created",
                table: "words",
                columns: new[] { "block_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_words_due_review",
                table: "words",
                column: "next_review_at");

            migrationBuilder.CreateIndex(
                name: "idx_words_sort",
                table: "words",
                columns: new[] { "block_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "idx_words_term_trgm",
                table: "words",
                column: "term")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "idx_words_translation_trgm",
                table: "words",
                column: "translation")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            // unaccent() is STABLE, not IMMUTABLE — wrap it so PostgreSQL allows it in index expressions
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION immutable_unaccent(text)
                RETURNS text AS $$
                  SELECT unaccent($1)
                $$ LANGUAGE SQL IMMUTABLE PARALLEL SAFE STRICT;
                """);

            migrationBuilder.Sql(
                "CREATE INDEX idx_words_fts ON words USING GIN " +
                "(to_tsvector('simple', immutable_unaccent(term) || ' ' || immutable_unaccent(translation)));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_words_fts;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS immutable_unaccent(text);");

            migrationBuilder.DropTable(
                name: "block_tags");

            migrationBuilder.DropTable(
                name: "words");

            migrationBuilder.DropTable(
                name: "word_blocks");
        }
    }
}
