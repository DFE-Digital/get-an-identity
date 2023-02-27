using System.Globalization;
using Bogus;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.UserImport;
using TeacherIdentity.DevBootstrap.Csv;
using static Bogus.DataSets.Name;

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

if (args.Contains("--generate-user-import"))
{
    Console.WriteLine();
    await GenerateUserImport();
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

async Task GenerateUserImport(int userCount = 10_000)
{
    Console.WriteLine($"Generating user import file with {userCount} users... ");

    // Generate all possible TRNs and randomise list to use to generate unique TRNs
    var trns = Enumerable.Range(1000000, 8999999);
    var randomizer = new Randomizer();
    var randomised = randomizer.Shuffle(trns);
    var enumerator = randomised.GetEnumerator();
    var trnGenerator = () =>
    {
        if (!enumerator.MoveNext())
        {
            enumerator.Reset();
            enumerator.MoveNext();
        }

        var trn = enumerator.Current;
        return trn.ToString();
    };

    var userImportRowFaker = new Faker<UserImportRow>("en")
            .RuleFor(r => r.Id, (f, u) => Guid.NewGuid().ToString())
            .RuleFor(r => r.FirstName, (f, u) => f.Name.FirstName(f.PickRandom<Gender>()))
            .RuleFor(r => r.LastName, (f, u) => f.Name.LastName())
            .RuleFor(i => i.EmailAddress, (f, i) => f.Internet.Email(i.FirstName, i.LastName, uniqueSuffix: randomizer.Number(1, 1000000).ToString()))
            .RuleFor(r => r.DateOfBirth, (f, u) => DateOnly.FromDateTime(f.Date.Between(new DateTime(1950, 1, 1), new DateTime(2002, 1, 1))).ToString("ddMMyyyy"))
            .RuleFor(r => r.Trn, (f, u) => trnGenerator());

    var userImportFilePath = Path.Combine(AppContext.BaseDirectory, $"test-user-import-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    using var writer = new StreamWriter(userImportFilePath);
    using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
    int logFrequency = userCount / 20;
    var userImportRowCount = 0;
    foreach (var userImportRow in userImportRowFaker.GenerateLazy(userCount))
    {
        await csv.WriteRecordsAsync(new[] { userImportRow });
        userImportRowCount++;

        if (userImportRowCount != 0 && userImportRowCount % logFrequency == 0)
        {
            Console.WriteLine($"Generated {userImportRowCount} user import rows.");
        }
    }

    // Log any unlogged totals
    if (userImportRowCount % logFrequency != 0)
    {
        Console.WriteLine($"Generated {userImportRowCount} user import rows.");
    }

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
