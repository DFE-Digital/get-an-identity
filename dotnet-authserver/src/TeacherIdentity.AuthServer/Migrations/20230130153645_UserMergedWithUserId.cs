using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class UserMergedWithUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "merged_with_user_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_merged_with_user_id",
                table: "users",
                column: "merged_with_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_users_users_merged_with_user_id",
                table: "users",
                column: "merged_with_user_id",
                principalTable: "users",
                principalColumn: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_users_merged_with_user_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_merged_with_user_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "merged_with_user_id",
                table: "users");
        }
    }
}
