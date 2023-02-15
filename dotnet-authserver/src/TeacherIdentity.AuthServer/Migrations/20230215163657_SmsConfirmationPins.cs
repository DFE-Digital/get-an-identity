using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class SmsConfirmationPins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sms_confirmation_pins",
                columns: table => new
                {
                    smsconfirmationpinid = table.Column<long>(name: "sms_confirmation_pin_id", type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mobilenumber = table.Column<string>(name: "mobile_number", type: "character varying(100)", maxLength: 100, nullable: false),
                    pin = table.Column<string>(type: "character(6)", fixedLength: true, maxLength: 6, nullable: false),
                    expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isactive = table.Column<bool>(name: "is_active", type: "boolean", nullable: false),
                    verifiedon = table.Column<DateTime>(name: "verified_on", type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sms_confirmation_pins", x => x.smsconfirmationpinid);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sms_confirmation_pins_mobile_number_pin",
                table: "sms_confirmation_pins",
                columns: new[] { "mobile_number", "pin" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sms_confirmation_pins");
        }
    }
}
