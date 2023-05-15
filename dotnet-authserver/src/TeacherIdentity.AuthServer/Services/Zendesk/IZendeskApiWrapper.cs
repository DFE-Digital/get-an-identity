using ZendeskApi.Client.Requests;
using ZendeskApi.Client.Responses;

namespace TeacherIdentity.AuthServer.Services.Zendesk;

public interface IZendeskApiWrapper
{
    Task<TicketResponse> CreateTicketAsync(TicketCreateRequest ticket, CancellationToken cancellationToken = default);
}
