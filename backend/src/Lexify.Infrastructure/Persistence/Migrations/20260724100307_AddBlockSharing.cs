using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "block_shares",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    block_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    copy_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_block_shares", x => x.id);
                    table.ForeignKey(
                        name: "fk_block_shares_block",
                        column: x => x.block_id,
                        principalTable: "word_blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_block_shares_owner",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_block_shares_block",
                table: "block_shares",
                column: "block_id");

            migrationBuilder.CreateIndex(
                name: "idx_block_shares_token",
                table: "block_shares",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_block_shares_owner_user_id",
                table: "block_shares",
                column: "owner_user_id");

            // Row-level security for the new table, matching the sibling user-owned tables (see
            // AddRlsPolicies). GRANT ... ON ALL TABLES ran before this table existed, so the
            // least-privilege lexify_app/lexify_admin roles need explicit grants here.
            migrationBuilder.Sql(@"
GRANT SELECT, INSERT, UPDATE, DELETE ON block_shares TO lexify_app;
GRANT ALL PRIVILEGES ON block_shares TO lexify_admin;");

            migrationBuilder.Sql("ALTER TABLE block_shares ENABLE ROW LEVEL SECURITY;");

            // Owner-scoped, like every other user-owned table: creating and revoking a link is the
            // owner's business.
            //
            // NOTE for whenever the app actually connects as lexify_app: reading a share by token, and
            // reading the block and words behind it, is by design a *cross-user* read — the whole point
            // of the feature. Under this policy (and pol_word_blocks_owner / pol_words_owner) those
            // reads would return nothing. Today they work because the app connects as the table owner,
            // which bypasses RLS, and app.current_user_id is never set. Switching to the restricted
            // role means adding a policy for the share-read path, not just flipping the connection
            // string.
            migrationBuilder.Sql(@"
CREATE POLICY pol_block_shares_owner ON block_shares
    FOR ALL TO lexify_app
    USING (owner_user_id = current_setting('app.current_user_id', TRUE)::UUID);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS pol_block_shares_owner ON block_shares;");

            migrationBuilder.DropTable(
                name: "block_shares");
        }
    }
}
