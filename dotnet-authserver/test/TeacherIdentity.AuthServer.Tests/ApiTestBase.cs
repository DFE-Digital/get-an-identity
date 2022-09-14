using IdentityModel.Client;

namespace TeacherIdentity.AuthServer.Tests;

public class ApiTestBase
{
    protected ApiTestBase(HostFixture hostFixture)
    {
        HostFixture = hostFixture;

        ApiKeyHttpClient = hostFixture.CreateClient();
        ApiKeyHttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test");

        HostFixture.ResetMocks();
    }

    public IClock Clock => HostFixture.Services.GetRequiredService<IClock>();

    public HostFixture HostFixture { get; }

    public HttpClient ApiKeyHttpClient { get; }

    public Task<HttpClient> CreateHttpClientWithToken(string scope)
    {
        var scopes = !string.IsNullOrEmpty(scope) ? new[] { scope } : Array.Empty<string>();
        return CreateHttpClientWithToken(scopes);
    }

    public async Task<HttpClient> CreateHttpClientWithToken(params string[] scopes)
    {
        var user = TestUsers.AdminUser1;
        var client = TestClients.Client1;

        var allScopes = new[] { "email", "profile", "openid" }.Concat(scopes).Distinct();

        var httpClient = HostFixture.CreateClient();

        var configuration = await httpClient.GetDiscoveryDocumentAsync(httpClient.BaseAddress!.ToString());
        if (configuration.IsError)
        {
            throw new Exception($"An error occurred while retrieving the configuration document: '{configuration.Error}'.");
        }

        var response = await httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest()
        {
            ClientId = client.ClientId,
            ClientSecret = client.ClientSecret,
            Scope = string.Join(" ", allScopes),
            Address = configuration.TokenEndpoint,
            UserName = user.EmailAddress,
            Password = "apasswordwehavetosupplybutdontuse"
        });

        if (response.IsError)
        {
            throw new Exception($"An error occurred while retrieving an access token: '{response.Error}'.");
        }

        var accessToken = response.AccessToken;
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        return httpClient;
    }

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();
}
