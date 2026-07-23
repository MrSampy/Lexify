using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "email_verified_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_verification_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    purpose = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    new_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_verification_tokens", x => x.id);
                    table.CheckConstraint("chk_email_verification_tokens_new_email", "(purpose = 'email_change' AND new_email IS NOT NULL) OR (purpose = 'signup' AND new_email IS NULL)");
                    table.CheckConstraint("chk_email_verification_tokens_purpose", "purpose IN ('signup', 'email_change')");
                    table.ForeignKey(
                        name: "fk_email_verification_tokens_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_email_verification_tokens_user",
                table: "email_verification_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_email_verification_token_hash",
                table: "email_verification_tokens",
                column: "token_hash",
                unique: true);

            // Grandfather every existing account. These users signed up before confirmation was a
            // requirement; locking them out of an app they already use would be a regression, not a
            // security win.
            migrationBuilder.Sql("UPDATE users SET email_verified_at = created_at;");

            // Row-level security for the new table, matching password_reset_tokens (see AddRlsPolicies).
            // GRANT ... ON ALL TABLES ran before this table existed, so the least-privilege roles need
            // explicit grants here.
            migrationBuilder.Sql(@"
GRANT SELECT, INSERT, UPDATE, DELETE ON email_verification_tokens TO lexify_app;
GRANT ALL PRIVILEGES ON email_verification_tokens TO lexify_admin;");

            migrationBuilder.Sql("ALTER TABLE email_verification_tokens ENABLE ROW LEVEL SECURITY;");

            // Confirmation happens while signed out, so the policy cannot key on the current user the
            // way the other user-owned tables do: the token itself is the credential, and it is only
            // ever looked up by its hash.
            migrationBuilder.Sql(@"
CREATE POLICY pol_email_verification_tokens_app ON email_verification_tokens
    FOR ALL TO lexify_app
    USING (TRUE);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP POLICY IF EXISTS pol_email_verification_tokens_app ON email_verification_tokens;");

            migrationBuilder.DropTable(
                name: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "email_verified_at",
                table: "users");
        }
    }
}
