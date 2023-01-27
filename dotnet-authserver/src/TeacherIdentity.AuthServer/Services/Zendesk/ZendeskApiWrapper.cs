using ZendeskApi.Client;
using ZendeskApi.Client.Requests;

namespace TeacherIdentity.AuthServer.Services.Zendesk;

public class ZendeskApiWrapper : IZendeskApiWrapper
{
    private readonly IZendeskClient _zendeskClient;

    public ZendeskApiWrapper(IZendeskClient zendeskClient)
    {
        _zendeskClient = zendeskClient;
    }

    public Task CreateTicketAsync(TicketCreateRequest ticket, CancellationToken cancellationToken = default) =>
        _zendeskClient.Tickets.CreateAsync(ticket, cancellationToken);
}
