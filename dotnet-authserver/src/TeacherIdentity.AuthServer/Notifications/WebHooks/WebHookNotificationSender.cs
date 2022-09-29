namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public class WebHookNotificationSender : IWebHookNotificationSender
{
    private readonly HttpClient _httpClient;

    public WebHookNotificationSender(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendNotification(string endpoint, string payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(payload)
        };

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to deliver web hook; received status code: {response.StatusCode}.\nBody:\n{body}");
        }
    }
}
