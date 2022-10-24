using Microsoft.Extensions.Options;

namespace TeacherIdentity.AuthServer.SmokeTests;

public class SmokeTestFixture
{
    public SmokeTestFixture(IOptions<SmokeTestOptions> optionsAccessor)
    {
        Options = optionsAccessor.Value;
        HttpClient = new HttpClient() { BaseAddress = new Uri(Options.BaseUrl) };
    }

    public HttpClient HttpClient { get; }

    public SmokeTestOptions Options { get; }
}
