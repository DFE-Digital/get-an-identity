using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications.Messages;

namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public class WebHookNotificationPublisher : INotificationPublisher
{
    private readonly IOptions<WebHookNotificationOptions> _optionsAccessor;

    public WebHookNotificationPublisher(IOptions<WebHookNotificationOptions> optionsAccessor)
    {
        _optionsAccessor = optionsAccessor;
    }

    public Task<WebHook[]> GetWebHooksForNotification(NotificationEnvelope notification)
    {
        // TODO Get this from DB
        return Task.FromResult(new[] { new WebHook() { Endpoint = "https://localhost:7236/webhook" } });
    }

    public virtual async Task PublishNotification(NotificationEnvelope notification)
    {
        var webHooks = await GetWebHooksForNotification(notification);

        foreach (var webHook in webHooks)
        {
            await PublishNotificationToWebHook(notification, webHook);
        }
    }

    public async Task PublishNotificationToWebHook(
        NotificationEnvelope notification,
        WebHook webHook)
    {
        // TODO Add logging, metrics

        // TODO Use a singleton HttpClient that doesn't follow redirects, doesn't store cookies, reasonable timeout etc.
        using var httpClient = new HttpClient();

        var response = await httpClient.PostAsync(
            webHook.Endpoint,
            JsonContent.Create(notification, options: _optionsAccessor.Value.SerializerOptions));

        // TODO Log response body, especially if it's a failure
        response.EnsureSuccessStatusCode();
    }
}
