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

    public TestClock Clock => (TestClock)HostFixture.Services.GetRequiredService<IClock>();

    public HostFixture HostFixture { get; }

    public HttpClient HttpClient { get; }

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();

    public AuthenticationStateHelper CreateAuthenticationStateHelper(
        Action<AuthenticationState>? configureAuthenticationState = null,
        string? scope = null) =>
            AuthenticationStateHelper.Create(configureAuthenticationState, HostFixture, scope ?? "trn");
}
