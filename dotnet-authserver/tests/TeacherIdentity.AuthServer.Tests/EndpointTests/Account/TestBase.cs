using Flurl;
using Microsoft.AspNetCore.DataProtection;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account;

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

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();

    public HttpClient HttpClient { get; }

    public SpyRegistry SpyRegistry => HostFixture.SpyRegistry;

    public ClientRedirectInfo CreateClientRedirectInfo() => CreateClientRedirectInfo(TestClients.Client1);

    public ClientRedirectInfo CreateClientRedirectInfo(TeacherIdentityApplicationDescriptor client)
    {
        var dataProtectionProvider = HostFixture.Services.GetRequiredService<IDataProtectionProvider>();
        var dataProtector = dataProtectionProvider.CreateProtector(nameof(ClientRedirectInfo));

        var clientId = client.ClientId!;
        string redirectUri = new Url(client.RedirectUris.First()).RemoveQuery();

        return new(dataProtector, clientId, redirectUri);
    }

    public string AppendQueryParameterSignature(Url url, params string[] parameterNames)
    {
        var queryStringSignatureHelper = HostFixture.Services.GetRequiredService<QueryStringSignatureHelper>();
        return queryStringSignatureHelper.AppendSignature(url, parameterNames);
    }
}
