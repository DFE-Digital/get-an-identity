using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class FixTrnVerificationLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                update users u
                set trn_verification_level = 1
                from (
                    select cast(payload->>'UserId' as uuid) user_id from authentication_states
                    where payload->>'FirstTimeSignInForEmail' = 'true'
                    and payload->>'Trn' is not null
                    and payload->'OAuthState'->>'TrnMatchPolicy' = '1'
                    and payload->>'UserId' is not null
                ) a
                where u.user_id = a.user_id
                and u.trn_verification_level != 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
