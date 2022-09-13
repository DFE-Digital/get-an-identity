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
await using var scope = serviceProvider.CreateAsyncScope();

using (var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>())
{
    await dbContext.Database.EnsureCreatedAsync();
}

var helper = new ClientConfigurationHelper(scope.ServiceProvider);
await helper.UpsertClients(clients);

Console.WriteLine("Configuration updated successfully.");

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
