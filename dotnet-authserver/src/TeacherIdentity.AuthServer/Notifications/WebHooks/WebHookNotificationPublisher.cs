using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications.Messages;

namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public class WebHookNotificationPublisher : INotificationPublisher
{
    private readonly IWebHookNotificationSender _sender;

    public WebHookNotificationPublisher(IWebHookNotificationSender sender)
    {
        _sender = sender;
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
            await _sender.SendNotification(notification, webHook);
        }
    }
}
