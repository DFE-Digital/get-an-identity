using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class JourneyTrnLookupStateNino : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "national_insurance_number",
                table: "journey_trn_lookup_states",
                type: "character(9)",
                fixedLength: true,
                maxLength: 9,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "national_insurance_number",
                table: "journey_trn_lookup_states");
        }
    }
}
