using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_call_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    call_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: true),
                    output_tokens = table.Column<int>(type: "integer", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    input_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_call_logs", x => x.id);
                    table.CheckConstraint("chk_ai_logs_duration", "duration_ms >= 0");
                    table.CheckConstraint("chk_ai_logs_provider", "provider IN ('ollama', 'openai')");
                    table.CheckConstraint("chk_ai_logs_type", "call_type IN ('format_words', 'generate_test', 'suggest_title')");
                    table.ForeignKey(
                        name: "fk_ai_logs_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    target_id = table.Column<string>(type: "text", nullable: true),
                    old_value = table.Column<string>(type: "jsonb", nullable: true),
                    new_value = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_admin",
                        column: x => x.admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_ai_logs_errors",
                table: "ai_call_logs",
                column: "created_at",
                descending: new bool[0],
                filter: "success = FALSE");

            migrationBuilder.CreateIndex(
                name: "idx_ai_logs_provider",
                table: "ai_call_logs",
                columns: new[] { "provider", "success" });

            migrationBuilder.CreateIndex(
                name: "idx_ai_logs_type",
                table: "ai_call_logs",
                columns: new[] { "call_type", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_ai_logs_user",
                table: "ai_call_logs",
                column: "user_id",
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_audit_action",
                table: "audit_logs",
                columns: new[] { "action", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_audit_admin",
                table: "audit_logs",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_created",
                table: "audit_logs",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_audit_target",
                table: "audit_logs",
                columns: new[] { "target_type", "target_id" });

            // EF Core merges HasIndex on the same column — add plain DESC index via raw SQL
            migrationBuilder.Sql("CREATE INDEX idx_ai_logs_created ON ai_call_logs (created_at DESC);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_ai_logs_created;");

            migrationBuilder.DropTable(
                name: "ai_call_logs");

            migrationBuilder.DropTable(
                name: "audit_logs");
        }
    }
}
