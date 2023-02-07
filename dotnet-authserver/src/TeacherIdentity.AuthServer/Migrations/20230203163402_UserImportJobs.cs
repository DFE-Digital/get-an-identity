using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class UserImportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_import_jobs",
                columns: table => new
                {
                    userimportjobid = table.Column<Guid>(name: "user_import_job_id", type: "uuid", nullable: false),
                    storedfilename = table.Column<string>(name: "stored_filename", type: "text", nullable: false),
                    originalfilename = table.Column<string>(name: "original_filename", type: "text", nullable: false),
                    userimportjobstatus = table.Column<int>(name: "user_import_job_status", type: "integer", nullable: false),
                    uploaded = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    imported = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_import_jobs", x => x.userimportjobid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_import_jobs");
        }
    }
}
