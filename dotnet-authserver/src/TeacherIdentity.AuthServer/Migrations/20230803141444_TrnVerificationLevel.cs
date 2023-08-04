using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class TrnVerificationLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "trn_verification_level",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "trn_association_source",
                keyValue: "0",
                column: "trn_verification_level",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trn_verification_level",
                table: "users");
        }
    }
}
