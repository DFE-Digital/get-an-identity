using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class UniqueEmailConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email_address",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_email_address",
                table: "users",
                column: "email_address",
                unique: true,
                filter: "is_deleted = false");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email_address",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_email_address",
                table: "users",
                column: "email_address",
                unique: true,
                filter: "is_deleted = true");
        }
    }
}
