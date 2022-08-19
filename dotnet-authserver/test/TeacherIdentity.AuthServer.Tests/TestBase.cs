using TeacherIdentity.AuthServer.State;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests;

public abstract partial class TestBase : IClassFixture<HostFixture>
{
    protected TestBase(HostFixture hostFixture)
    {
        HostFixture = hostFixture;

        HttpClient = hostFixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions()
        {
            AllowAutoRedirect = false
        });

        HostFixture.ResetMocks();
    }

    public IClock Clock => HostFixture.Services.GetRequiredService<IClock>();

    public HostFixture HostFixture { get; }

    public HttpClient HttpClient { get; }

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();

    public AuthenticationStateHelper CreateAuthenticationStateHelper(Action<AuthenticationState>? configureAuthenticationState = null)
    {
        var testAuthStateProvider = (TestAuthenticationStateProvider)HostFixture.Services.GetRequiredService<IAuthenticationStateProvider>();
        return AuthenticationStateHelper.Create(configureAuthenticationState, testAuthStateProvider);
    }
}
