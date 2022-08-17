namespace TeacherIdentity.AuthServer.Tests;

public class ApiTestBase : IClassFixture<HostFixture>
{
    protected ApiTestBase(HostFixture hostFixture)
    {
        HostFixture = hostFixture;

        HttpClient = hostFixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions()
        {
            AllowAutoRedirect = false
        });
        HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test");

        HostFixture.ResetMocks();
    }

    public IClock Clock => HostFixture.Services.GetRequiredService<IClock>();

    public HostFixture HostFixture { get; }

    public HttpClient HttpClient { get; }

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();
}
