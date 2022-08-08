using TeacherIdentity.AuthServer.State;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests;

public abstract class TestBase : IClassFixture<HostFixture>
{
    protected TestBase(HostFixture hostFixture)
    {
        HostFixture = hostFixture;

        HttpClient = hostFixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions()
        {
            AllowAutoRedirect = false
        });
    }

    public HostFixture HostFixture { get; }

    public HttpClient HttpClient { get; }

    public AuthenticationStateHelper CreateAuthenticationStateHelper(Action<AuthenticationState>? configureAuthenticationState = null)
    {
        var testAuthStateProvider = (TestAuthenticationStateProvider)HostFixture.Services.GetRequiredService<IAuthenticationStateProvider>();
        return AuthenticationStateHelper.Create(configureAuthenticationState, testAuthStateProvider);
    }
}
