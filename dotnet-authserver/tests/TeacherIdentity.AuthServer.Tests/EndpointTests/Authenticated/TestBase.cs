using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Authenticated;

public partial class TestBase
{
    protected TestBase(HostFixture hostFixture)
    {
        HostFixture = hostFixture;
        HostFixture.SetUserId(TestUsers.DefaultUser.UserId);

        HttpClient = HostFixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions()
        {
            AllowAutoRedirect = false
        });

        HostFixture.ResetMocks();
        HostFixture.InitEventObserver();
    }

    public IClock Clock => HostFixture.Services.GetRequiredService<IClock>();

    public CaptureEventObserver EventObserver => HostFixture.EventObserver;

    public HostFixture HostFixture { get; }

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();

    public HttpClient HttpClient { get; }
}
