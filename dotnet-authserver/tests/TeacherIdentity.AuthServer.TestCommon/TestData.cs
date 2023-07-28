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
    private static readonly ConcurrentBag<string> _usedTrns = new();
    private static readonly ConcurrentBag<string> _usedEmails = new();
    private static readonly ConcurrentBag<string> _mobileNumbers;
    private static readonly ConcurrentBag<string> _usedTrnTokens = new();

    static TestData()
    {
        _mobileNumbers = GeneratePhoneNumbers();
    }

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
            while (_usedTrns.Contains(trn));

            _usedTrns.Add(trn);
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

        } while (_usedEmails.Contains(email));

        _usedEmails.Add(email);
        return email;
    }

    public string GenerateUniqueMobileNumber()
    {
        if (!_mobileNumbers.TryTake(out var mobileNumber))
        {
            throw new Exception("Exhausted mobile numbers.");
        }

        return mobileNumber;
    }

    public async Task<TrnTokenModel?> GetTrnToken(string trnToken)
    {
        return await WithDbContext(async dbContext =>
        {
            return await dbContext.TrnTokens.SingleOrDefaultAsync(t => t.TrnToken == trnToken);
        });
    }

    public async Task<User> GetUser(Guid userId)
    {
        return await WithDbContext(async dbContext =>
        {
            return await dbContext.Users.SingleAsync(u => u.UserId == userId);
        });
    }

    public async Task<TrnTokenModel> GenerateTrnToken(string trn, DateTime? expires = null, string? email = null)
    {
        expires ??= _clock.UtcNow.AddYears(1);

        var trnToken = new TrnTokenModel()
        {
            TrnToken = GenerateUniqueTrnTokenValue(),
            Trn = trn,
            Email = email ?? GenerateUniqueEmail(),
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
        } while (_usedTrnTokens.Contains(trnToken));

        _usedTrnTokens.Add(trnToken);
        return trnToken;
    }

    public async Task EnsureEstablishmentDomain(string invalidEmailSuffix)
    {
        await WithDbContext(async dbContext =>
        {
            var establishmentDomain = new EstablishmentDomain
            {
                DomainName = invalidEmailSuffix
            };

            try
            {
                dbContext.EstablishmentDomains.Add(establishmentDomain);
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueIndexViolation("pk_establishment_domains"))
            {
                // ignored
            }
        });
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

    private static ConcurrentBag<string> GeneratePhoneNumbers()
    {
        // https://www.ofcom.org.uk/phones-telecoms-and-internet/information-for-industry/numbering/numbers-for-drama

        return new ConcurrentBag<string>(GenerateNumberRange("07700 ", 900000, 900999)
            .Concat(GenerateNumberRange("0113 496 ", 0000, 0999)));

        static IEnumerable<string> GenerateNumberRange(string prefix, int from, int to)
        {
            for (var i = from; i <= to; i++)
            {
                yield return $"{prefix} {i}";
            }
        }
    }
}
