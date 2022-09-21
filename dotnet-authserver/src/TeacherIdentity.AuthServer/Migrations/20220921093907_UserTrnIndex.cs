using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class UserTrnIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_trn",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_trn",
                table: "users",
                column: "trn",
                unique: true,
                filter: "is_deleted = false and trn is not null");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_trn",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_trn",
                table: "users",
                column: "trn",
                unique: true,
                filter: "is_deleted = true and trn is not null");
        }
    }
}
