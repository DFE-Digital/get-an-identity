using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class UserTrnLookupStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "trn_lookup_status",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("update users set trn_lookup_status = case when trn is null then 1 else 2 end where user_type = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trn_lookup_status",
                table: "users");
        }
    }
}
