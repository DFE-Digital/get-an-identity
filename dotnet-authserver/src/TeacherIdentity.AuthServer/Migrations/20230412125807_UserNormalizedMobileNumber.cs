using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class UserNormalizedMobileNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_mobile_number",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "normalized_mobile_number",
                table: "users",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_mobile_number",
                table: "users",
                column: "normalized_mobile_number",
                unique: true,
                filter: "is_deleted = false and normalized_mobile_number is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_mobile_number",
                table: "users");

            migrationBuilder.DropColumn(
                name: "normalized_mobile_number",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_mobile_number",
                table: "users",
                column: "mobile_number",
                unique: true,
                filter: "is_deleted = false and mobile_number is not null");
        }
    }
}
