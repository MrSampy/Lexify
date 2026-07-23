using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_number = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    subject = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    rating = table.Column<short>(type: "smallint", nullable: true),
                    contact_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "new"),
                    admin_note = table.Column<string>(type: "text", nullable: true),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback", x => x.id);
                    table.CheckConstraint("chk_feedback_rating", "(type = 'review' AND rating BETWEEN 1 AND 5) OR (type <> 'review' AND rating IS NULL)");
                    table.CheckConstraint("chk_feedback_status", "status IN ('new', 'in_progress', 'resolved')");
                    table.CheckConstraint("chk_feedback_type", "type IN ('suggestion', 'bug', 'review', 'question')");
                    table.ForeignKey(
                        name: "fk_feedback_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "feedback_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feedback_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_feedback_attachments_feedback",
                        column: x => x.feedback_id,
                        principalTable: "feedback",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_feedback_status_created",
                table: "feedback",
                columns: new[] { "status", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_feedback_ticket_number",
                table: "feedback",
                column: "ticket_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_feedback_user_id",
                table: "feedback",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_feedback_attachments_feedback_id",
                table: "feedback_attachments",
                column: "feedback_id");

            // Row-level security for the new tables, matching the sibling user-owned tables (see
            // AddRlsPolicies). GRANT ... ON ALL TABLES ran before these tables existed, so the
            // least-privilege lexify_app/lexify_admin roles need explicit grants here.
            migrationBuilder.Sql(@"
GRANT SELECT, INSERT, UPDATE, DELETE ON feedback TO lexify_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON feedback_attachments TO lexify_app;
GRANT ALL PRIVILEGES ON feedback TO lexify_admin;
GRANT ALL PRIVILEGES ON feedback_attachments TO lexify_admin;");

            migrationBuilder.Sql("ALTER TABLE feedback             ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE feedback_attachments ENABLE ROW LEVEL SECURITY;");

            // Triage is an admin job, so no policy is granted to lexify_app for reading other
            // people's tickets — lexify_admin carries no restricting policy and sees everything.
            migrationBuilder.Sql(@"
CREATE POLICY pol_feedback_owner ON feedback
    FOR ALL TO lexify_app
    USING (user_id = current_setting('app.current_user_id', TRUE)::UUID);");

            migrationBuilder.Sql(@"
CREATE POLICY pol_feedback_attachments_owner ON feedback_attachments
    FOR ALL TO lexify_app
    USING (
        feedback_id IN (
            SELECT id FROM feedback
            WHERE user_id = current_setting('app.current_user_id', TRUE)::UUID
        )
    );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_feedback_attachments_owner ON feedback_attachments;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_feedback_owner ON feedback;");

            migrationBuilder.DropTable(
                name: "feedback_attachments");

            migrationBuilder.DropTable(
                name: "feedback");
        }
    }
}
