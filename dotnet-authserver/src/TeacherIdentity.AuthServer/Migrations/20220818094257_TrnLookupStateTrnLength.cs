using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class TrnLookupStateTrnLength : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "trn",
                table: "journey_trn_lookup_states",
                type: "character(7)",
                fixedLength: true,
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "trn",
                table: "journey_trn_lookup_states",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character(7)",
                oldFixedLength: true,
                oldMaxLength: 7,
                oldNullable: true);
        }
    }
}
