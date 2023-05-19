using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
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
    private static readonly ConcurrentBag<string> _trnTokens = new();

    public TestData(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _clock = serviceProvider.GetRequiredService<IClock>();
    }

    // https://stackoverflow.com/a/30290754
    public static byte[] JpegImage { get; } =
    {
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00,
        0xFF, 0xDB, 0x00, 0x43, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC2, 0x00, 0x0B, 0x08, 0x00, 0x01, 0x00, 0x01, 0x01, 0x01,
        0x11, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x01, 0x3F, 0x10
    };

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

    public async Task<TrnTokenModel?> GetTrnToken(string trnToken)
    {
        return await WithDbContext(async dbContext =>
        {
            return await dbContext.TrnTokens.SingleOrDefaultAsync(t => t.TrnToken == trnToken);
        });
    }

    public async Task<TrnTokenModel> GenerateTrnToken(string trn, DateTime? expires = null)
    {
        expires ??= _clock.UtcNow.AddYears(1);

        var trnToken = new TrnTokenModel()
        {
            TrnToken = GenerateUniqueTrnTokenValue(),
            Trn = trn,
            Email = GenerateUniqueEmail(),
            CreatedUtc = _clock.UtcNow,
            ExpiresUtc = expires.Value
        };

        await WithDbContext(async dbContext =>
        {
            dbContext.TrnTokens.Add(trnToken);
            await dbContext.SaveChangesAsync();
        });

        return trnToken;
    }

    public string GenerateUniqueTrnTokenValue()
    {
        string trnToken;

        do
        {
            var buffer = new byte[64];
            RandomNumberGenerator.Fill(buffer);
            trnToken = Convert.ToHexString(buffer);
        } while (_trnTokens.Contains(trnToken));

        _trnTokens.Add(trnToken);
        return trnToken;
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
