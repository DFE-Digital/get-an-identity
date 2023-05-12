using ZendeskApi.Client;
using ZendeskApi.Client.Requests;
using ZendeskApi.Client.Responses;

namespace TeacherIdentity.AuthServer.Services.Zendesk;

public class ZendeskApiWrapper : IZendeskApiWrapper
{
    private readonly IZendeskClient _zendeskClient;

    public ZendeskApiWrapper(IZendeskClient zendeskClient)
    {
        _zendeskClient = zendeskClient;
    }

    public Task<TicketResponse> CreateTicketAsync(TicketCreateRequest ticket, CancellationToken cancellationToken = default) =>
        _zendeskClient.Tickets.CreateAsync(ticket, cancellationToken);
}
