using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.TestCommon;

public partial class TestData
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IClock _clock;
    private static readonly Random _random = new();
    private static readonly ConcurrentBag<string> _trns = new();
    private static readonly ConcurrentBag<string> _emails = new();
    private static readonly ConcurrentBag<string> _mobileNumbers = new();

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

    public string GenerateUniqueMobileNumber()
    {
        lock (_random)
        {
            string mobileNumber;

            do
            {
                // See https://www.ofcom.org.uk/phones-telecoms-and-internet/information-for-industry/numbering/numbers-for-drama
                mobileNumber = $"07700 {_random.NextInt64(900000, 900999)}";
            }
            while (_mobileNumbers.Contains(mobileNumber));

            _mobileNumbers.Add(mobileNumber);
            return mobileNumber;
        }
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
