using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Oidc;

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
        HostFixture.InitEventObserver();
    }

    public TestClock Clock => (TestClock)HostFixture.Services.GetRequiredService<IClock>();

    public HostFixture HostFixture { get; }

    public HttpClient HttpClient { get; }

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();

    public Task<AuthenticationStateHelper> CreateAuthenticationStateHelper(
        AuthenticationStateConfiguration configure,
        string? additionalScopes,
        TrnRequirementType? trnRequirementType = null,
        TrnMatchPolicy? trnMatchPolicy = null,
        TeacherIdentityApplicationDescriptor? client = null,
        string? registrationToken = null) =>
        AuthenticationStateHelper.Create(configure, HostFixture, additionalScopes, trnRequirementType, trnMatchPolicy, client, registrationToken);
}
