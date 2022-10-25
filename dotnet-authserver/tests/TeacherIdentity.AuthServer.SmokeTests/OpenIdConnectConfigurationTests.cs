namespace TeacherIdentity.AuthServer.SmokeTests;

public class OpenIdConnectConfigurationTests : IClassFixture<SmokeTestFixture>
{
    private readonly SmokeTestFixture _fixture;

    public OpenIdConnectConfigurationTests(SmokeTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MetadataEndpoint_LoadsSuccessfully()
    {
        var response = await _fixture.HttpClient.GetAsync("/.well-known/openid-configuration");
        response.EnsureSuccessStatusCode();
    }
}
