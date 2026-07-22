using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenConversationsAndPersistScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_conversation_messages_order",
                table: "conversation_messages");

            migrationBuilder.AddColumn<int>(
                name: "points",
                table: "conversations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "stars",
                table: "conversations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "words_used",
                table: "conversations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_conversation_messages_order",
                table: "conversation_messages",
                columns: new[] { "conversation_id", "sort_order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_conversation_messages_order",
                table: "conversation_messages");

            migrationBuilder.DropColumn(
                name: "points",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "stars",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "words_used",
                table: "conversations");

            migrationBuilder.CreateIndex(
                name: "idx_conversation_messages_order",
                table: "conversation_messages",
                columns: new[] { "conversation_id", "sort_order" });
        }
    }
}
