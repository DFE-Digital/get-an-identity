using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class JourneyTrnLookupStateUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "journey_trn_lookup_states",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_journey_trn_lookup_states_user_id",
                table: "journey_trn_lookup_states",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_journey_trn_lookup_states_users_user_id",
                table: "journey_trn_lookup_states",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_journey_trn_lookup_states_users_user_id",
                table: "journey_trn_lookup_states");

            migrationBuilder.DropIndex(
                name: "ix_journey_trn_lookup_states_user_id",
                table: "journey_trn_lookup_states");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "journey_trn_lookup_states");
        }
    }
}
