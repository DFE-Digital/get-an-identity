using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications.Messages;

namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public class WebHookNotificationSender : IWebHookNotificationSender
{
    private readonly IOptions<WebHookNotificationOptions> _optionsAccessor;

    public WebHookNotificationSender(IOptions<WebHookNotificationOptions> optionsAccessor)
    {
        _optionsAccessor = optionsAccessor;
    }

    public async Task SendNotification(NotificationEnvelope notification, WebHook webHook)
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
