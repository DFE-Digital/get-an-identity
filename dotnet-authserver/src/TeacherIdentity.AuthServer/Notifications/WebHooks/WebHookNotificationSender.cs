using System.Security.Cryptography;
using System.Text;

namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public class WebHookNotificationSender : IWebHookNotificationSender
{
    private const string ContentType = "application/json";

    private readonly HttpClient _httpClient;

    public WebHookNotificationSender(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public static string CalculateSignature(string secret, string payload)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        return Convert.ToHexString(HMACSHA256.HashData(secretBytes, payloadBytes));
    }

    public async Task SendNotification(Guid notificationId, string endpoint, string payload, string secret)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(payload, new System.Net.Http.Headers.MediaTypeHeaderValue(ContentType))
        };

        request.Headers.Add("X-TeacherIdentity-NotificationId", notificationId.ToString());

        if (secret != string.Empty)
        {
            request.Headers.Add("X-Hub-Signature-256", CalculateSignature(secret, payload));
        }

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to deliver web hook; received status code: {response.StatusCode}.\nBody:\n{body}");
        }
    }
}
