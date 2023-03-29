using Flurl;
using TeacherIdentity.AuthServer.Helpers;
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

    public TestClock Clock => (TestClock)HostFixture.Services.GetRequiredService<IClock>();

    public CaptureEventObserver EventObserver => HostFixture.EventObserver;

    public HostFixture HostFixture { get; }

    public SpyRegistry SpyRegistry => HostFixture.SpyRegistry;

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();

    public HttpClient HttpClient { get; }

    public string AppendQueryParameterSignature(Url url, params string[] parameterNames)
    {
        var queryStringSignatureHelper = HostFixture.Services.GetRequiredService<QueryStringSignatureHelper>();
        return queryStringSignatureHelper.AppendSignature(url, parameterNames);
    }
}
