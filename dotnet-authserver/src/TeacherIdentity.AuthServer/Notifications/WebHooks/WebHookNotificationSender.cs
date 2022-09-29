namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public class WebHookNotificationSender : IWebHookNotificationSender
{
    public async Task SendNotification(string endpoint, string payload)
    {
        // TODO Add logging, metrics

        // TODO Use a singleton HttpClient that doesn't follow redirects, doesn't store cookies, reasonable timeout etc.
        using var httpClient = new HttpClient();

        var response = await httpClient.PostAsync(endpoint, new StringContent(payload));

        // TODO Log response body, especially if it's a failure
        response.EnsureSuccessStatusCode();
    }
}
