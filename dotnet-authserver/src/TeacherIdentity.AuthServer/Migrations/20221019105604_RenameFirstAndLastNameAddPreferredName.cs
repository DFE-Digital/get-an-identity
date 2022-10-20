using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class RenameFirstAndLastNameAddPreferredName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "journey_trn_lookup_states",
                newName: "official_last_name");

            migrationBuilder.RenameColumn(
                name: "first_name",
                table: "journey_trn_lookup_states",
                newName: "official_first_name");

            migrationBuilder.AddColumn<string>(
                name: "preferred_first_name",
                table: "journey_trn_lookup_states",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_last_name",
                table: "journey_trn_lookup_states",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preferred_first_name",
                table: "journey_trn_lookup_states");

            migrationBuilder.DropColumn(
                name: "preferred_last_name",
                table: "journey_trn_lookup_states");

            migrationBuilder.RenameColumn(
                name: "official_last_name",
                table: "journey_trn_lookup_states",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "official_first_name",
                table: "journey_trn_lookup_states",
                newName: "first_name");
        }
    }
}
