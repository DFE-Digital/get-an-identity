using TeacherIdentity.AuthServer.Tests.Infrastructure;
using static TeacherIdentity.AuthServer.Tests.AuthenticationStateHelper;

namespace TeacherIdentity.AuthServer.Tests;

public abstract partial class TestBase
{
    protected TestBase(HostFixture hostFixture)
    {
        HostFixture = hostFixture;

        HttpClient = hostFixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions()
        {
            AllowAutoRedirect = false
        });

        HostFixture.ResetMocks();
        HostFixture.ResetUseNewTrnLookupJourney();
        HostFixture.InitEventObserver();
    }

    public TestClock Clock => (TestClock)HostFixture.Services.GetRequiredService<IClock>();

    public CaptureEventObserver EventObserver => HostFixture.EventObserver;

    public HostFixture HostFixture { get; }

    public HttpClient HttpClient { get; }

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();

    public Task<AuthenticationStateHelper> CreateAuthenticationStateHelper(Func<Configure, Func<AuthenticationState, Task>> configure, string? additionalScopes = "trn") =>
        AuthenticationStateHelper.Create(configure, HostFixture, additionalScopes);
}
