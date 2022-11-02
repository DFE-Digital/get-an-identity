using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    public partial class BackfillUserRegisteredWithClientId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
update users u
set registered_with_client_id = e.client_id
from (
	select cast(payload -> 'User' ->> 'UserId' as uuid) user_id, payload ->> 'ClientId' client_id from events
	where event_name = 'UserRegisteredEvent'
) e
where u.registered_with_client_id is null and u.user_id = e.user_id;
");

            migrationBuilder.Sql(@"
update users u
set registered_with_client_id = e.client_id
from (
	select distinct on (payload ->> 'UserId') cast(payload ->> 'UserId' as uuid) user_id, payload ->> 'ClientId' client_id from events
	where event_name = 'UserSignedIn' and payload ->> 'ClientId' is not null
	order by payload ->> 'UserId', created
) e
where u.registered_with_client_id is null and u.user_id = e.user_id;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
