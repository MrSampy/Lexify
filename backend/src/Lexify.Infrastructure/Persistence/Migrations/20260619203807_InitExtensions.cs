using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pgcrypto\";");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pg_trgm\";");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"unaccent\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS \"unaccent\";");
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS \"pg_trgm\";");
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS \"pgcrypto\";");
        }
    }
}
