using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZendeskApi.Client;
using ZendeskApi.Client.Models;

namespace TeacherIdentity.AuthServer.Services.Zendesk;

public class ZendeskHealthCheck : IHealthCheck
{
    private readonly IZendeskClient _zendeskClient;

    public ZendeskHealthCheck(IZendeskClient zendeskClient)
    {
        _zendeskClient = zendeskClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var tickets = await _zendeskClient.Tickets.GetAllAsync(new CursorPager() { Size = 10 }, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}
