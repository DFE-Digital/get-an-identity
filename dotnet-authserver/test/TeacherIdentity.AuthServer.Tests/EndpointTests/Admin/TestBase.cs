using TeacherIdentity.AuthServer.State;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class TestBase : IAsyncLifetime
{
    protected TestBase(HostFixture hostFixture)
    {
        HostFixture = hostFixture;

        HostFixture.ResetMocks();
    }

    public IClock Clock => HostFixture.Services.GetRequiredService<IClock>();

    public HostFixture HostFixture { get; }

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();

    public HttpClient? AuthenticatedHttpClient { get; private set; }

    public HttpClient? AuthenticatedHttpClientWithNoRoles { get; private set; }

    public async Task<HttpClient> CreateAuthenticatedHttpClient(Guid userId)
    {
        var authenticationState = new AuthenticationState(
            journeyId: Guid.NewGuid(),
            UserRequirements.StaffUserType,
            postSignInUrl: "/admin");

        var authenticationStateProvider = (TestAuthenticationStateProvider)HostFixture.Services.GetRequiredService<IAuthenticationStateProvider>();
        authenticationStateProvider.SetAuthenticationState(httpContext: null, authenticationState);

        var httpClient = HostFixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions()
        {
            AllowAutoRedirect = false
        });

        await HostFixture.SignInUser(authenticationState.JourneyId, httpClient, userId, firstTimeSignInForEmail: false);

        return httpClient;
    }

    public HttpClient CreateAuthenticatedHttpClient() =>
        HostFixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions()
        {
            AllowAutoRedirect = false
        });

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        AuthenticatedHttpClient = await CreateAuthenticatedHttpClient(TestUsers.AdminUserWithAllRoles.UserId);
        AuthenticatedHttpClientWithNoRoles = await CreateAuthenticatedHttpClient(TestUsers.AdminUserWithNoRoles.UserId);
    }
}
