using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRlsPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Create roles ──────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'lexify_app') THEN
        CREATE ROLE lexify_app NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'lexify_admin') THEN
        CREATE ROLE lexify_admin NOLOGIN;
    END IF;
END $$;");

            // ── Grant permissions ─────────────────────────────────────────────────
            migrationBuilder.Sql(@"
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO lexify_app;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO lexify_app;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO lexify_admin;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO lexify_admin;");

            // ── Enable RLS ────────────────────────────────────────────────────────
            migrationBuilder.Sql("ALTER TABLE word_blocks     ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE words           ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE tests           ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE test_attempts   ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE attempt_answers ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE tags            ENABLE ROW LEVEL SECURITY;");

            // ── RLS policies ──────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE POLICY pol_word_blocks_owner ON word_blocks
    FOR ALL TO lexify_app
    USING (user_id = current_setting('app.current_user_id', TRUE)::UUID);");

            migrationBuilder.Sql(@"
CREATE POLICY pol_words_owner ON words
    FOR ALL TO lexify_app
    USING (
        block_id IN (
            SELECT id FROM word_blocks
            WHERE user_id = current_setting('app.current_user_id', TRUE)::UUID
        )
    );");

            migrationBuilder.Sql(@"
CREATE POLICY pol_tests_owner ON tests
    FOR ALL TO lexify_app
    USING (user_id = current_setting('app.current_user_id', TRUE)::UUID);");

            migrationBuilder.Sql(@"
CREATE POLICY pol_test_attempts_owner ON test_attempts
    FOR ALL TO lexify_app
    USING (user_id = current_setting('app.current_user_id', TRUE)::UUID);");

            migrationBuilder.Sql(@"
CREATE POLICY pol_attempt_answers_owner ON attempt_answers
    FOR ALL TO lexify_app
    USING (
        attempt_id IN (
            SELECT id FROM test_attempts
            WHERE user_id = current_setting('app.current_user_id', TRUE)::UUID
        )
    );");

            migrationBuilder.Sql(@"
CREATE POLICY pol_tags_owner ON tags
    FOR ALL TO lexify_app
    USING (user_id = current_setting('app.current_user_id', TRUE)::UUID);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_tags_owner ON tags;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_attempt_answers_owner ON attempt_answers;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_test_attempts_owner ON test_attempts;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_tests_owner ON tests;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_words_owner ON words;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_word_blocks_owner ON word_blocks;");

            migrationBuilder.Sql("ALTER TABLE tags            DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE attempt_answers DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE test_attempts   DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE tests           DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE words           DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE word_blocks     DISABLE ROW LEVEL SECURITY;");

            migrationBuilder.Sql("DROP ROLE IF EXISTS lexify_admin;");
            migrationBuilder.Sql("DROP ROLE IF EXISTS lexify_app;");
        }
    }
}
