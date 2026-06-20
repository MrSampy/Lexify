using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTriggersAndFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── fn_update_word_count + trigger ────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_update_word_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE word_blocks
        SET word_count = word_count + 1,
            updated_at = NOW()
        WHERE id = NEW.block_id;

    ELSIF TG_OP = 'DELETE' THEN
        UPDATE word_blocks
        SET word_count = GREATEST(word_count - 1, 0),
            updated_at = NOW()
        WHERE id = OLD.block_id;

    ELSIF TG_OP = 'UPDATE' AND OLD.block_id != NEW.block_id THEN
        UPDATE word_blocks
        SET word_count = GREATEST(word_count - 1, 0), updated_at = NOW()
        WHERE id = OLD.block_id;

        UPDATE word_blocks
        SET word_count = word_count + 1, updated_at = NOW()
        WHERE id = NEW.block_id;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
CREATE TRIGGER trg_words_word_count
AFTER INSERT OR DELETE OR UPDATE OF block_id ON words
FOR EACH ROW EXECUTE FUNCTION fn_update_word_count();");

            // ── fn_update_question_count + trigger ────────────────────────────────
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_update_question_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE tests
        SET question_count = COALESCE(question_count, 0) + 1
        WHERE id = NEW.test_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE tests
        SET question_count = GREATEST(COALESCE(question_count, 1) - 1, 0)
        WHERE id = OLD.test_id;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
CREATE TRIGGER trg_questions_count
AFTER INSERT OR DELETE ON questions
FOR EACH ROW EXECUTE FUNCTION fn_update_question_count();");

            // ── fn_set_updated_at + triggers ──────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();");

            migrationBuilder.Sql(@"
CREATE TRIGGER trg_word_blocks_updated_at
    BEFORE UPDATE ON word_blocks
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();");

            // ── fn_normalize_email + trigger ──────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_normalize_email()
RETURNS TRIGGER AS $$
BEGIN
    NEW.email = LOWER(TRIM(NEW.email));
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
CREATE TRIGGER trg_users_normalize_email
    BEFORE INSERT OR UPDATE OF email ON users
    FOR EACH ROW EXECUTE FUNCTION fn_normalize_email();");

            // ── fn_touch_user_activity ────────────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_touch_user_activity(p_user_id UUID)
RETURNS void AS $$
BEGIN
    UPDATE users
    SET last_active_at = NOW()
    WHERE id = p_user_id
      AND (last_active_at IS NULL OR last_active_at < NOW() - INTERVAL '5 minutes');
END;
$$ LANGUAGE plpgsql;");

            // ── fn_anonymize_deleted_users ────────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_anonymize_deleted_users()
RETURNS integer AS $$
DECLARE
    affected integer;
BEGIN
    UPDATE users
    SET email        = 'deleted_' || id::text || '@removed.invalid',
        display_name = NULL,
        password_hash = '',
        updated_at   = NOW()
    WHERE status = 'deleted'
      AND deleted_at < NOW() - INTERVAL '30 days'
      AND email NOT LIKE 'deleted_%@removed.invalid';
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$ LANGUAGE plpgsql;");

            // ── fn_cleanup_refresh_tokens ─────────────────────────────────────────
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_cleanup_refresh_tokens()
RETURNS integer AS $$
DECLARE
    deleted_count integer;
BEGIN
    DELETE FROM refresh_tokens
    WHERE (expires_at < NOW() OR revoked_at IS NOT NULL)
      AND created_at < NOW() - INTERVAL '7 days';
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_users_normalize_email ON users;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_word_blocks_updated_at ON word_blocks;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_users_updated_at ON users;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_questions_count ON questions;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_words_word_count ON words;");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_cleanup_refresh_tokens();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_anonymize_deleted_users();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_touch_user_activity(UUID);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_normalize_email();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_set_updated_at();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_update_question_count();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_update_word_count();");
        }
    }
}
