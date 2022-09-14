using TeacherIdentity.AuthServer.TestCommon;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var testConfiguration = new TestConfiguration();
        var dbHelper = new DbHelper(testConfiguration.Configuration.GetConnectionString("DefaultConnection"));
        var hostFixture = new HostFixture(testConfiguration, dbHelper);

        hostFixture.Initialize().GetAwaiter().GetResult();

        services.AddSingleton(testConfiguration);
        services.AddSingleton(dbHelper);
        services.AddSingleton(hostFixture);
    }
}
