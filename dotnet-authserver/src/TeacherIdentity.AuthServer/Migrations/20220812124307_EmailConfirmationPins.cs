using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class EmailConfirmationPins : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_confirmation_pins",
                columns: table => new
                {
                    email_confirmation_pin_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    pin = table.Column<string>(type: "character(6)", fixedLength: true, maxLength: 6, nullable: false),
                    expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_confirmation_pins", x => x.email_confirmation_pin_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_confirmation_pins_email_pin",
                table: "email_confirmation_pins",
                columns: new[] { "email", "pin" },
                unique: true)
                .Annotation("Npgsql:IndexInclude", new[] { "expires", "is_active" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_confirmation_pins");
        }
    }
}
