using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

var configuration = new ConfigurationManager();

configuration
    .AddUserSecrets<Program>()
    .AddJsonFile("appsettings.Development.json");

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

        options.AddApplicationStore<TeacherIdentityApplicationStore>();
        options.ReplaceApplicationManager<TeacherIdentityApplicationManager>();
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
