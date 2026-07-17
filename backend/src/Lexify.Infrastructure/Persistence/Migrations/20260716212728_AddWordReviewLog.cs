using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWordReviewLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "word_review_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    block_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<short>(type: "smallint", nullable: false),
                    quality = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ease_factor_after = table.Column<double>(type: "double precision", nullable: false),
                    interval_days_after = table.Column<int>(type: "integer", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_word_review_logs", x => x.id);
                    table.CheckConstraint("chk_review_logs_quality", "quality BETWEEN 0 AND 5");
                    table.CheckConstraint("chk_review_logs_source", "source IN ('review', 'test')");
                    table.ForeignKey(
                        name: "fk_review_logs_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_review_logs_user_time",
                table: "word_review_logs",
                columns: new[] { "user_id", "reviewed_at" });

            // Row-level security for the new table, matching the sibling user-owned tables
            // (see AddRlsPolicies). GRANT ... ON ALL TABLES ran before this table existed, so the
            // least-privilege lexify_app/lexify_admin roles need explicit grants here.
            migrationBuilder.Sql(@"
GRANT SELECT, INSERT, UPDATE, DELETE ON word_review_logs TO lexify_app;
GRANT ALL PRIVILEGES ON word_review_logs TO lexify_admin;");

            migrationBuilder.Sql("ALTER TABLE word_review_logs ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql(@"
CREATE POLICY pol_review_logs_owner ON word_review_logs
    FOR ALL TO lexify_app
    USING (user_id = current_setting('app.current_user_id', TRUE)::UUID);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_review_logs_owner ON word_review_logs;");
            migrationBuilder.DropTable(
                name: "word_review_logs");
        }
    }
}
