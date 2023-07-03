using TeacherIdentity.AuthServer.Tests.Infrastructure;
using TeacherIdentity.AuthServer.Tests.WebHooks;

namespace TeacherIdentity.AuthServer.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var testConfiguration = new TestConfiguration();
        var dbHelper = new DbHelper(testConfiguration.Configuration.GetConnectionString("DefaultConnection") ??
            throw new Exception("Connection string DefaultConnection is missing."));
        var hostFixture = new HostFixture(testConfiguration, dbHelper);
        hostFixture.Initialize().GetAwaiter().GetResult();

        var webhooksHostFixture = new WebHooksHostFixture(testConfiguration, dbHelper);
        webhooksHostFixture.Initialize().GetAwaiter().GetResult();

        services.AddSingleton(testConfiguration);
        services.AddSingleton(dbHelper);
        services.AddSingleton(hostFixture);
        services.AddSingleton(webhooksHostFixture);
    }
}
