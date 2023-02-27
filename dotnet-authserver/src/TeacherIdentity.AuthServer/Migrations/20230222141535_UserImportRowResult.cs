using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class UserImportRowResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "errors",
                table: "user_import_job_rows",
                newName: "notes");

            migrationBuilder.AddColumn<int>(
                name: "user_import_row_result",
                table: "user_import_job_rows",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_import_row_result",
                table: "user_import_job_rows");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "user_import_job_rows",
                newName: "errors");
        }
    }
}
