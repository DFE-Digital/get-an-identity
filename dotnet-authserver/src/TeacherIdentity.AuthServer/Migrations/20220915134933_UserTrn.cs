using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class UserTrn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "trn",
                table: "users",
                type: "character(7)",
                fixedLength: true,
                maxLength: 7,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_trn",
                table: "users",
                column: "trn",
                unique: true,
                filter: "is_deleted = true and trn is not null");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_trn",
                table: "users");

            migrationBuilder.DropColumn(
                name: "trn",
                table: "users");
        }
    }
}
