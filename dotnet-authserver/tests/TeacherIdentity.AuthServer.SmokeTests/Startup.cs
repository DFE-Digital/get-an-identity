using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TeacherIdentity.AuthServer.SmokeTests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        services.AddSingleton(configuration);

        services.AddOptions<SmokeTestOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations();
    }
}
