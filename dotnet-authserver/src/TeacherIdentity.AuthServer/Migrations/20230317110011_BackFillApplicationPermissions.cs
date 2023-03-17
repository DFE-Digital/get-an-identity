using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class BackFillApplicationPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE applications
                SET permissions = REPLACE(permissions, '"scp:email"', '"scp:email", "scp:phone"');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE applications
                SET permissions = REPLACE(REPLACE(permissions, '"scp:phone", ', ''), ', "scp:phone"', '');
                """);
        }
    }
}
