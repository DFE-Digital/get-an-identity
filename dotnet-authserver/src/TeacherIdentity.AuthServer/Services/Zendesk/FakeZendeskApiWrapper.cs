using ZendeskApi.Client.Requests;

namespace TeacherIdentity.AuthServer.Services.Zendesk;

public class FakeZendeskApiWrapper : IZendeskApiWrapper
{
    private readonly ILogger<FakeZendeskApiWrapper> _logger;

    public FakeZendeskApiWrapper(ILogger<FakeZendeskApiWrapper> logger)
    {
        _logger = logger;
    }

    public Task CreateTicketAsync(TicketCreateRequest ticket, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FakeZendeskApiWrapper received request to create Zendesk ticket: {@TicketRequest}", ticket);
        return Task.CompletedTask;
    }
}
