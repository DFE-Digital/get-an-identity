using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class TrnLookupStatusCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                update users set trn_lookup_status = 1
                where trn_lookup_status = 0 and completed_trn_lookup is not null;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_trn_lookup_status",
                table: "users",
                sql: "(completed_trn_lookup is null and trn is null) or trn_lookup_status is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_trn_lookup_status",
                table: "users");
        }
    }
}
