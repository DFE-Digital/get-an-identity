using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class TrnTokensTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "trn_tokens",
                columns: table => new
                {
                    trn_token = table.Column<string>(type: "character(128)", fixedLength: true, maxLength: 128, nullable: false),
                    trn = table.Column<string>(type: "character(7)", fixedLength: true, maxLength: 7, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trn_tokens", x => x.trn_token);
                });

            migrationBuilder.CreateIndex(
                name: "ix_trn_tokens_email_address",
                table: "trn_tokens",
                column: "email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trn_tokens");
        }
    }
}
