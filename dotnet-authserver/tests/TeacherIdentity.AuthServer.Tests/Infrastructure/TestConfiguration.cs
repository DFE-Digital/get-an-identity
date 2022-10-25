namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class TestConfiguration
{
    public IConfiguration Configuration { get; } =
        new ConfigurationBuilder()
            .AddUserSecrets<HostFixture>()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
}
