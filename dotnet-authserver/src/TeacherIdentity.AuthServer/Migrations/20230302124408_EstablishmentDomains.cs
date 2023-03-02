using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class EstablishmentDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "establishment_domains",
                columns: table => new
                {
                    domainname = table.Column<string>(name: "domain_name", type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_establishment_domains", x => x.domainname);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "establishment_domains");
        }
    }
}
