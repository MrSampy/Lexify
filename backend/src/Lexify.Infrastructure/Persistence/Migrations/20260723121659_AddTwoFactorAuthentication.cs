using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "two_factor_enabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "login_two_factor_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_two_factor_codes", x => x.id);
                    table.CheckConstraint("chk_login_two_factor_codes_attempts", "attempts >= 0");
                    table.ForeignKey(
                        name: "fk_login_two_factor_codes_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_login_two_factor_codes_user",
                table: "login_two_factor_codes",
                column: "user_id");

            // No data backfill: email-code 2FA needs no enrollment, so existing admins (forced on by
            // role) simply get a code emailed on their next sign-in. two_factor_enabled defaults false,
            // which is correct for opt-in users.

            // Row-level security for the new table, matching email_verification_tokens. GRANT ... ON ALL
            // TABLES ran before this table existed, so the least-privilege roles need explicit grants.
            migrationBuilder.Sql(@"
GRANT SELECT, INSERT, UPDATE, DELETE ON login_two_factor_codes TO lexify_app;
GRANT ALL PRIVILEGES ON login_two_factor_codes TO lexify_admin;");

            migrationBuilder.Sql("ALTER TABLE login_two_factor_codes ENABLE ROW LEVEL SECURITY;");

            // Code verification happens while signed out (between the password step and the code step),
            // so the policy cannot key on the current user: the emailed code is the credential, always
            // looked up scoped by user_id.
            migrationBuilder.Sql(@"
CREATE POLICY pol_login_two_factor_codes_app ON login_two_factor_codes
    FOR ALL TO lexify_app
    USING (TRUE);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP POLICY IF EXISTS pol_login_two_factor_codes_app ON login_two_factor_codes;");

            migrationBuilder.DropTable(
                name: "login_two_factor_codes");

            migrationBuilder.DropColumn(
                name: "two_factor_enabled",
                table: "users");
        }
    }
}
