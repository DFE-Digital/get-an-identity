using System.Globalization;
using Bogus;
using CsvHelper.Configuration;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static Bogus.DataSets.Name;
using WorkforceDataApi.DevUtils.Csv;

var configuration = new ConfigurationManager();

configuration
    .AddUserSecrets<Program>()
    .AddJsonFile("appsettings.Development.json");

var pgConnectionString = GetRequiredConfigurationValue("ConnectionStrings:DefaultConnection");

IServiceCollection services = new ServiceCollection();

services.AddDbContext<TeacherIdentityServerDbContext>(options =>
{
    TeacherIdentityServerDbContext.ConfigureOptions(options, pgConnectionString);
});

services.AddSingleton<IEventObserver, NoopEventObserver>();

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

await CreateDatabase();
Console.WriteLine();
await ConfigureAdminAccount();
Console.WriteLine();
await ConfigureClients();

if (args.Contains("--generate-test-users"))
{
    Console.WriteLine();
    await GenerateTestUsers();
}

if (args.Contains("--import-test-users"))
{
    Console.WriteLine();
    await ImportTestUsers();
}

async Task CreateDatabase()
{
    Console.Write("Checking database exists... ");

    await WithDbContext(dbContext => dbContext.Database.MigrateAsync());

    Console.WriteLine("done.");
}

async Task ConfigureAdminAccount()
{
    Console.Write("Adding developer account... ");

    var developerEmail = configuration["DeveloperEmail"];

    if (string.IsNullOrEmpty(developerEmail))
    {
        Console.WriteLine("failed.");
        WriteWarningLine("To add an admin account set the 'DeveloperEmail' configuration key to your email address in user secrets.");
        return;
    }

    await WithDbContext(async dbContext =>
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.EmailAddress == developerEmail);

        if (user is null)
        {
            Console.WriteLine();
            var firstName = Prompt("Your first name:");
            var lastName = Prompt("Your last name:");

            dbContext.Users.Add(new User()
            {
                Created = DateTime.UtcNow,
                EmailAddress = developerEmail,
                FirstName = firstName,
                LastName = lastName,
                StaffRoles = StaffRoles.All,
                Updated = DateTime.UtcNow,
                UserId = Guid.NewGuid(),
                UserType = UserType.Staff
            });

            Console.Write($"Creating new user with email {developerEmail}... ");
        }
        else
        {
            user.StaffRoles = StaffRoles.All;
            user.Updated = DateTime.UtcNow;

            Console.Write($"updating existing user with email {developerEmail}... ");
        }

        await dbContext.SaveChangesAsync();
    });

    Console.WriteLine("done.");
}

async Task ConfigureClients()
{
    Console.Write("Configuring OIDC clients... ");

    var clients = configuration.GetSection("Clients").Get<ClientConfiguration[]>();

    if (clients is null || clients.Length == 0)
    {
        Console.WriteLine("failed.");
        WriteWarningLine("No clients found in configuration.");
        return;
    }

    await using var scope = serviceProvider.CreateAsyncScope();
    var helper = new ClientConfigurationHelper(scope.ServiceProvider);
    await helper.UpsertClients(clients);

    Console.WriteLine("done.");
}

async Task GenerateTestUsers()
{
    const int TestUsersCount = 100;
    Console.Write("Generating test users... ");

    await WithDbContext(async dbContext =>
    {
        var userFaker = new Faker<User>("en")
            .RuleFor(u => u.UserId, (f, u) => Guid.NewGuid())
            .RuleFor(u => u.Created, (f, u) => DateTime.UtcNow)
            .RuleFor(u => u.Updated, (f, u) => u.Created)
            .RuleFor(u => u.FirstName, (f, u) => f.Name.FirstName(f.PickRandom<Gender>()))
            .RuleFor(u => u.LastName, (f, u) => f.Name.LastName())
            .RuleFor(u => u.EmailAddress, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
            .RuleFor(u => u.Trn, (f, u) => f.Random.Number(1000000, 9999999).ToString())
            .RuleFor(u => u.DateOfBirth, (f, u) => DateOnly.FromDateTime(f.Date.Between(new DateTime(1950, 1, 1), new DateTime(2000, 1, 1))))
            .RuleFor(u => u.UserType, (f, u) => UserType.Teacher);

        foreach (var user in userFaker.GenerateLazy(TestUsersCount))
        {
            dbContext.Users.Add(user);
        }

        await dbContext.SaveChangesAsync();
    });

    Console.WriteLine("done.");
}

async Task ImportTestUsers(string csvFileName = "test-users.csv")
{
    Console.Write("Importing test users... ");

    using var reader = new StreamReader(csvFileName);
    using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
    csv.Context.RegisterClassMap<UserReaderMap>();

    await WithDbContext(async dbContext =>
    {
        int i = 0;
        await foreach (var item in csv.GetRecordsAsync<User>())
        {
            dbContext.Users.Add(item);
            if (i != 0 && i % 10_000 == 0)
            {
                await dbContext.SaveChangesAsync();
                Console.WriteLine($"Saved {i} users in teacher identity database.");
            }

            i++;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"Saved {i} users in teacher identity database.");
        }
    });

    Console.WriteLine("done.");
}

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

async Task WithDbContext(Func<TeacherIdentityServerDbContext, Task> action)
{
    await using var scope = serviceProvider.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();
    await action(dbContext);
}

static void WriteWarningLine(string message)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(message);
    Console.ResetColor();
}

static string Prompt(string prelude)
{
    prelude = prelude.TrimEnd(' ') + " ";

    string? answer;

    do
    {
        Console.Write(prelude);
        answer = Console.ReadLine();
    }
    while (string.IsNullOrEmpty(answer));

    return answer;
}
