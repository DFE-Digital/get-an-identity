using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class EventPayloadGinIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("alter table events alter column payload type jsonb using payload::jsonb;");

            migrationBuilder.Sql("alter table events alter column created type timestamp with time zone using cast(created as text)::timestamp without time zone at time zone 'Etc/UTC'");

            migrationBuilder.CreateIndex(
                name: "ix_events_payload",
                table: "events",
                column: "payload")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_events_payload",
                table: "events");

            migrationBuilder.Sql("alter table events alter column created type using created::jsonb");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created",
                table: "events",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
