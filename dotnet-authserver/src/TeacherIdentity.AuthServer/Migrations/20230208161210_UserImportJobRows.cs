using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class UserImportJobRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_import_job_rows",
                columns: table => new
                {
                    userimportjobid = table.Column<Guid>(name: "user_import_job_id", type: "uuid", nullable: false),
                    rownumber = table.Column<int>(name: "row_number", type: "integer", nullable: false),
                    id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    rawdata = table.Column<string>(name: "raw_data", type: "text", nullable: true),
                    userid = table.Column<Guid>(name: "user_id", type: "uuid", nullable: true),
                    errors = table.Column<List<string>>(type: "varchar[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_import_job_rows", x => new { x.userimportjobid, x.rownumber });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_import_job_rows");
        }
    }
}
