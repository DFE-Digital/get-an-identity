using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class BackfillUpdatedByClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                update events set payload = payload || '{ "UpdatedByClientId": "get-an-identity-support"}'::jsonb
                where event_name = 'UserUpdatedEvent' and payload->'UpdatedByClientId' is null;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                update events set payload = payload - 'UpdatedByClientId'
                where event_name = 'UserUpdatedEvent';
                """);
        }
    }
}
