using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationRaiseTrnResolutionSupportTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "web_hook_message_types",
                table: "webhooks",
                type: "integer",
                nullable: false,
                defaultValue: 7,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 3);

            migrationBuilder.AddColumn<bool>(
                name: "raise_trn_resolution_support_tickets",
                table: "applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "raise_trn_resolution_support_tickets",
                table: "applications");

            migrationBuilder.AlterColumn<int>(
                name: "web_hook_message_types",
                table: "webhooks",
                type: "integer",
                nullable: false,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 7);
        }
    }
}
