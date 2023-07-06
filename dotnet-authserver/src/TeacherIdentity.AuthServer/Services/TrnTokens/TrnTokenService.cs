using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services.TrnTokens;

public class TrnTokenService
{
    private readonly TrnTokenOptions _options;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public TrnTokenService(
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IOptions<TrnTokenOptions> optionsAccessor)
    {
        _options = optionsAccessor.Value;
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<TrnTokenModel> GenerateToken(
        string email,
        string trn,
        string? apiClientId,
        Guid? currentUserId)
    {
        if (apiClientId is null && currentUserId is null)
        {
            throw new ArgumentException($"Exactly one of {nameof(apiClientId)} and {nameof(currentUserId)} should be specified.");
        }

        string trnToken;
        do
        {
            var buffer = new byte[8];
            RandomNumberGenerator.Fill(buffer);
            trnToken = Convert.ToHexString(buffer).ToLower();
        } while (await _dbContext.TrnTokens.AnyAsync(t => t.TrnToken == trnToken));

        var created = _clock.UtcNow;
        var expires = created.AddDays(_options.TokenLifetimeDays);

        var model = new TrnTokenModel()
        {
            TrnToken = trnToken,
            Trn = trn,
            Email = email,
            CreatedUtc = created,
            ExpiresUtc = expires,
        };

        _dbContext.TrnTokens.Add(model);

        _dbContext.AddEvent(new TrnTokenAddedEvent()
        {
            AddedByApiClientId = apiClientId,
            AddedByUserId = currentUserId,
            CreatedUtc = created,
            ExpiresUtc = expires,
            Email = email,
            Trn = trn,
            TrnToken = trnToken
        });

        await _dbContext.SaveChangesAsync();

        return model;
    }
}
