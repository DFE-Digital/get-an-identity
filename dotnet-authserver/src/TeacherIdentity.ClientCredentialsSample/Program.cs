using System.Text.Json;

var httpClient = new HttpClient();

var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7236/connect/token");
request.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
{
    { "grant_type", "client_credentials" },
    { "client_id", "client2" },
    { "client_secret", "another-big-secret" },
});
var response = await httpClient.SendAsync(request);
response.EnsureSuccessStatusCode();

var json = await response.Content.ReadAsStringAsync();
var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

Console.WriteLine(tokenResponse!.AccessToken);


public class TokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
}
