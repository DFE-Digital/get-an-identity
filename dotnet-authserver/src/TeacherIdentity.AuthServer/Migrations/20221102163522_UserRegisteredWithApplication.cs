using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class UserRegisteredWithApplication : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "registered_with_client_id",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_users_application_registered_with_client_id",
                table: "users",
                column: "registered_with_client_id",
                principalTable: "applications",
                principalColumn: "client_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_application_registered_with_client_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "registered_with_client_id",
                table: "users");
        }
    }
}
