using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeacherIdentity.AuthServer.Clients;
using TeacherIdentity.AuthServer.Models;

var configuration = new ConfigurationManager();

configuration.AddCommandLine(
    args,
    switchMappings: new Dictionary<string, string>()
    {
        { "--environment", "EnvironmentName" },
        { "--connection-string", "ConnectionStrings__DefaultConnection" }
    });

var environmentName = GetRequiredConfigurationValue("EnvironmentName").ToLower();

if (environmentName == "local")
{
    configuration
        .AddUserSecrets<Program>()
        .AddJsonFile("appsettings.Local.json");
}

var pgConnectionString = GetRequiredConfigurationValue("ConnectionStrings:DefaultConnection");
var clients = configuration.GetSection("Clients").Get<ClientConfiguration[]>();

if (clients is null || clients.Length == 0)
{
    Console.WriteLine($"No clients found.");
    return;
}

IServiceCollection services = new ServiceCollection();

services.AddDbContext<TeacherIdentityServerDbContext>(options =>
{
    TeacherIdentityServerDbContext.ConfigureOptions(options, pgConnectionString);
});

services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<TeacherIdentityServerDbContext>()
            .ReplaceDefaultEntities<Application, Authorization, Scope, Token, string>();
    });

var serviceProvider = services.BuildServiceProvider();

var helper = new ClientConfigurationHelper(serviceProvider);
await helper.UpsertClients(clients);

string GetRequiredConfigurationValue(string key)
{
    var value = configuration[key];

    if (string.IsNullOrEmpty(value))
    {
        Console.Error.WriteLine($"Missing configuration entry for '{key}'.");
        Environment.Exit(1);
    }

    return value;
}
