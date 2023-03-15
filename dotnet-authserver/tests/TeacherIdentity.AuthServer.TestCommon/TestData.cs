using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.TestCommon;

public partial class TestData
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IClock _clock;
    private readonly Random _random = new();
    private readonly ConcurrentBag<string> _trns = new();
    private readonly ConcurrentBag<string> _emails = new();

    public TestData(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _clock = serviceProvider.GetRequiredService<IClock>();
    }

    public string GenerateTrn()
    {
        lock (_random)
        {
            string trn;

            do
            {
                trn = _random.Next(minValue: 1000000, maxValue: 1999999).ToString();
            }
            while (_trns.Contains(trn));

            _trns.Add(trn);
            return trn;
        }
    }

    public string GenerateUniqueEmail(string? prefix = null, string? suffix = null)
    {
        string email;

        // if both prefix and suffix are defined, email is fully specified
        Debug.Assert(prefix is null || suffix is null);

        do
        {
            email = Faker.Internet.Email();

            if (prefix is not null)
            {
                email = prefix + email.Substring(email.IndexOf("@", StringComparison.Ordinal));
            } 
            else if (suffix is not null)
            {
                email = email.Substring(0, 1 + email.IndexOf("@", StringComparison.Ordinal)) + suffix;
            }
        } while (_emails.Contains(email));

        _emails.Add(email);
        return email;
    }

    public async Task<T> WithDbContext<T>(Func<TeacherIdentityServerDbContext, Task<T>> action)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();
        return await action(dbContext);
    }

    public async Task WithDbContext(Func<TeacherIdentityServerDbContext, Task> action) =>
        await WithDbContext(async dbContext =>
        {
            await action(dbContext);
            return 0;
        });
}
