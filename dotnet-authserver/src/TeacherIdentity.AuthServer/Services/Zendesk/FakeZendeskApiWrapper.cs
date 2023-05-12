using ZendeskApi.Client.Models;
using ZendeskApi.Client.Requests;
using ZendeskApi.Client.Responses;

namespace TeacherIdentity.AuthServer.Services.Zendesk;

public class FakeZendeskApiWrapper : IZendeskApiWrapper
{
    private readonly ILogger<FakeZendeskApiWrapper> _logger;

    public FakeZendeskApiWrapper(ILogger<FakeZendeskApiWrapper> logger)
    {
        _logger = logger;
    }

    public Task<TicketResponse> CreateTicketAsync(TicketCreateRequest ticket, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FakeZendeskApiWrapper received request to create Zendesk ticket: {@TicketRequest}", ticket);
        return Task.FromResult(new TicketResponse() { Ticket = new Ticket() { Id = 1234567 } });
    }
}
