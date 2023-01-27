using ZendeskApi.Client.Requests;

namespace TeacherIdentity.AuthServer.Services.Zendesk;

public interface IZendeskApiWrapper
{
    Task CreateTicketAsync(TicketCreateRequest ticket, CancellationToken cancellationToken = default);
}
