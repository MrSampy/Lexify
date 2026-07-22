using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<short>(type: "smallint", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    scenario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "active"),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    target_word_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false, defaultValueSql: "'{}'::uuid[]"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.id);
                    table.CheckConstraint("chk_conversations_status", "status IN ('active', 'ended')");
                    table.ForeignKey(
                        name: "fk_conversations_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversation_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_messages", x => x.id);
                    table.CheckConstraint("chk_conversation_messages_role", "role IN ('user', 'assistant')");
                    table.ForeignKey(
                        name: "fk_conversation_messages_conversation",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_conversation_messages_order",
                table: "conversation_messages",
                columns: new[] { "conversation_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "idx_conversations_created",
                table: "conversations",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_conversations_user_id",
                table: "conversations",
                column: "user_id");

            // Allow the two new AI call types (conversation, analyze_conversation) that "Talk to Lexi"
            // writes to ai_call_logs — the DB check constraint enumerates the permitted values.
            migrationBuilder.DropCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs");

            migrationBuilder.AddCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs",
                sql: "call_type IN ('format_words', 'generate_test', 'generate_fill_sentences', 'generate_distractors', 'generate_definitions', 'suggest_title', 'conversation', 'analyze_conversation')");

            // "conversation" (12 chars) exceeds the original varchar(10) source column and isn't in the
            // source check — widen the column and extend the constraint so conversation-sourced reviews fit.
            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "word_review_logs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.DropCheckConstraint(
                name: "chk_review_logs_source",
                table: "word_review_logs");

            migrationBuilder.AddCheckConstraint(
                name: "chk_review_logs_source",
                table: "word_review_logs",
                sql: "source IN ('review', 'test', 'conversation')");

            // Row-level security for the new tables, matching the sibling user-owned tables (see
            // AddRlsPolicies). GRANT ... ON ALL TABLES ran before these tables existed, so the
            // least-privilege lexify_app/lexify_admin roles need explicit grants here.
            migrationBuilder.Sql(@"
GRANT SELECT, INSERT, UPDATE, DELETE ON conversations TO lexify_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON conversation_messages TO lexify_app;
GRANT ALL PRIVILEGES ON conversations TO lexify_admin;
GRANT ALL PRIVILEGES ON conversation_messages TO lexify_admin;");

            migrationBuilder.Sql("ALTER TABLE conversations         ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE conversation_messages ENABLE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
CREATE POLICY pol_conversations_owner ON conversations
    FOR ALL TO lexify_app
    USING (user_id = current_setting('app.current_user_id', TRUE)::UUID);");

            migrationBuilder.Sql(@"
CREATE POLICY pol_conversation_messages_owner ON conversation_messages
    FOR ALL TO lexify_app
    USING (
        conversation_id IN (
            SELECT id FROM conversations
            WHERE user_id = current_setting('app.current_user_id', TRUE)::UUID
        )
    );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_conversation_messages_owner ON conversation_messages;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_conversations_owner ON conversations;");

            migrationBuilder.DropCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs");

            migrationBuilder.AddCheckConstraint(
                name: "chk_ai_logs_type",
                table: "ai_call_logs",
                sql: "call_type IN ('format_words', 'generate_test', 'generate_fill_sentences', 'generate_distractors', 'generate_definitions', 'suggest_title')");

            migrationBuilder.DropCheckConstraint(
                name: "chk_review_logs_source",
                table: "word_review_logs");

            migrationBuilder.AddCheckConstraint(
                name: "chk_review_logs_source",
                table: "word_review_logs",
                sql: "source IN ('review', 'test')");

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "word_review_logs",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.DropTable(
                name: "conversation_messages");

            migrationBuilder.DropTable(
                name: "conversations");
        }
    }
}
