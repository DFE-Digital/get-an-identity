using System.Text.Json;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class Api : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public Api(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
    }

    [Fact]
    public async Task SignInWithUserReadScope_CanCallReadSupportEndpointSuccessfully()
    {
        var adminUser = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: new[] { StaffRoles.GetAnIdentityAdmin });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.UserRead);

        await page.SubmitEmailPage(adminUser.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(adminUser);

        // Call API endpoint that requires user:read scope
        using var apiHttpClient = new HttpClient()
        {
            BaseAddress = new Uri(HostFixture.AuthServerBaseUrl)
        };
        var apiRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users");
        apiRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _hostFixture.CapturedAccessTokens.Last());
        var apiResponse = await apiHttpClient.SendAsync(apiRequest);
        apiResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetTokenViaClientCredentialsWithReadScope_CanCallReadSupportEndpointSuccessfully()
    {
        using var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(HostFixture.AuthServerBaseUrl)
        };

        // Get a token
        var token = await GetTokenViaClientCredentials(httpClient, "user:read");

        // Call API endpoint that requires user:read scope
        var apiRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users");
        apiRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var apiResponse = await httpClient.SendAsync(apiRequest);
        apiResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SignInWithUserWriteScope_CanCallWriteSupportEndpointSuccessfully()
    {
        var adminUser = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: new[] { StaffRoles.GetAnIdentityAdmin });
        var teacherUser = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.UserWrite);

        await page.SubmitEmailPage(adminUser.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(adminUser);

        // Call API endpoint that requires user:write scope
        using var apiHttpClient = new HttpClient()
        {
            BaseAddress = new Uri(HostFixture.AuthServerBaseUrl)
        };
        var apiRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/{teacherUser.UserId}")
        {
            Content = JsonContent.Create(new { })
        };
        apiRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _hostFixture.CapturedAccessTokens.Last());
        var apiResponse = await apiHttpClient.SendAsync(apiRequest);
    }

    [Fact]
    public async Task GetTokenViaClientCredentialsWithWriteScope_CanCallWriteSupportEndpointSuccessfully()
    {
        var teacherUser = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        using var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(HostFixture.AuthServerBaseUrl)
        };

        // Get a token
        var token = await GetTokenViaClientCredentials(httpClient, "user:read");

        // Call API endpoint that requires user:read scope
        var apiRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/{teacherUser.UserId}")
        {
            Content = JsonContent.Create(new { })
        };
        apiRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var apiResponse = await httpClient.SendAsync(apiRequest);
    }

    private static async Task<string> GetTokenViaClientCredentials(HttpClient httpClient, string scope)
    {
        var tokenResponse = await httpClient.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "client_id", "testcc" },
                { "client_secret", "super-secret" },
                { "grant_type", "client_credentials" },
                { "scope", scope }
            }));
        tokenResponse.EnsureSuccessStatusCode();

        return (await tokenResponse.Content.ReadFromJsonAsync<JsonDocument>())!.RootElement.GetProperty("access_token").GetString()!;
    }
}
