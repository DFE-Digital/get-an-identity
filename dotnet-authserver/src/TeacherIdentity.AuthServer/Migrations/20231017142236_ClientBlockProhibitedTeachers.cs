﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class ClientBlockProhibitedTeachers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "block_prohibited_teachers",
                table: "applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "block_prohibited_teachers",
                table: "applications");
        }
    }
}
