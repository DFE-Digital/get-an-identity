using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class EmailConfirmationPinVerifiedOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_confirmation_pins_email_pin",
                table: "email_confirmation_pins");

            migrationBuilder.AddColumn<DateTime>(
                name: "verified_on",
                table: "email_confirmation_pins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_confirmation_pins_email_pin",
                table: "email_confirmation_pins",
                columns: new[] { "email", "pin" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_confirmation_pins_email_pin",
                table: "email_confirmation_pins");

            migrationBuilder.DropColumn(
                name: "verified_on",
                table: "email_confirmation_pins");

            migrationBuilder.CreateIndex(
                name: "ix_email_confirmation_pins_email_pin",
                table: "email_confirmation_pins",
                columns: new[] { "email", "pin" },
                unique: true)
                .Annotation("Npgsql:IndexInclude", new[] { "expires", "is_active" });
        }
    }
}
