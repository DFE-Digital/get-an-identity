using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class CaseInsensitiveEmailAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("create collation case_insensitive (provider = icu, locale = 'und-u-ks-level2', deterministic = false);");

            migrationBuilder.AlterColumn<string>(
                name: "email_address",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "email_address",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "case_insensitive");

            migrationBuilder.Sql("drop collation case_insensitive;");
        }
    }
}
