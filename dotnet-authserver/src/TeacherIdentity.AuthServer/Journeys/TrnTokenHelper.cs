using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.UserSearch;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Journeys;

public class TrnTokenHelper
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IConfiguration _configuration;
    private readonly IUserSearchService _userSearchService;

    public TrnTokenHelper(
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IDqtApiClient dqtApiClient,
        IConfiguration configuration,
        IUserSearchService userSearchService)
    {
        _dbContext = dbContext;
        _clock = clock;
        _dqtApiClient = dqtApiClient;
        _configuration = configuration;
        _userSearchService = userSearchService;
    }

    public async Task<EnhancedTrnToken?> GetValidTrnToken(OpenIddictRequest request)
    {
        var requestedTrnToken = request["trn_token"];

        if (_configuration.GetValue("RegisterWithTrnTokenEnabled", false) && requestedTrnToken.HasValue)
        {
            return await GetValidTrnTokenModel(requestedTrnToken.Value.Value as string);
        }

        return null;
    }

    public void InitializeAuthenticationStateForSignedInUser(
        User signedInUser,
        AuthenticationState authenticationState,
        EnhancedTrnToken trnToken)
    {
        if (signedInUser.EmailAddress == trnToken.Email)
        {
            authenticationState.OnSignedInUserProvided(signedInUser);
            if (signedInUser.Trn is null)
            {
                authenticationState.OnTrnTokenProvided(trnToken);
            }
        }
        else
        {
            authenticationState.OnTrnTokenProvided(trnToken);
        }
    }

    public void InitializeAuthenticationStateForExistingUser(
        User existingValidUser,
        AuthenticationState authenticationState,
        EnhancedTrnToken trnToken)
    {
        authenticationState.OnSignedInUserProvided(existingValidUser);

        if (existingValidUser.Trn is null)
        {
            authenticationState.OnTrnTokenProvided(trnToken);
        }
    }

    public async Task<User?> GetExistingValidUserForToken(EnhancedTrnToken trnToken)
    {
        return await _dbContext.Users.SingleOrDefaultAsync(u => u.EmailAddress == trnToken.Email);
    }

    public async Task<User?> GetExistingAccountMatchForToken(EnhancedTrnToken trnToken)
    {
        var users = await _userSearchService.FindUsers(trnToken.FirstName, trnToken.LastName, trnToken.DateOfBirth);
        return users.Length > 0 ? users[0] : null;
    }

    public async Task ApplyTrnTokenToUser(Guid? userId, string trnTokenValue)
    {
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId && u.UserType != UserType.Staff);
        var trnToken = await _dbContext.TrnTokens.SingleAsync(t => t.TrnToken == trnTokenValue);

        if (user.Trn is null)
        {
            user = await UpdateUserTrn(user, trnToken.Trn);
        }

        if (user.Trn == trnToken.Trn)
        {
            await InvalidateTrnToken(trnToken.TrnToken, user.UserId);
        }
    }

    public async Task InvalidateTrnToken(string trnTokenValue, Guid userId)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"update trn_tokens set user_id = {userId} where trn_token = {trnTokenValue};");
    }

    private async Task<EnhancedTrnToken?> GetValidTrnTokenModel(string? trnTokenValue)
    {
        var trnToken = await _dbContext.TrnTokens.SingleOrDefaultAsync(t => t.TrnToken == trnTokenValue && t.ExpiresUtc > _clock.UtcNow);

        if (trnToken is null || trnToken.UserId is not null)
        {
            return null;
        }

        var teacher = await _dqtApiClient.GetTeacherByTrn(trnToken.Trn);

        if (teacher is null)
        {
            return null;
        }

        return new EnhancedTrnToken()
        {
            TrnToken = trnToken.TrnToken,
            Trn = trnToken.Trn,
            Email = trnToken.Email,
            CreatedUtc = trnToken.CreatedUtc,
            ExpiresUtc = trnToken.ExpiresUtc,
            UserId = trnToken.UserId,
            FirstName = teacher.FirstName,
            MiddleName = teacher.MiddleName,
            LastName = teacher.LastName,
            DateOfBirth = teacher.DateOfBirth,
        };
    }

    private async Task<User> UpdateUserTrn(User user, string trn)
    {
        user.Trn = trn;
        user.TrnLookupStatus = TrnLookupStatus.Found;
        user.TrnAssociationSource = TrnAssociationSource.TrnToken;
        user.Updated = _clock.UtcNow;

        _dbContext.AddEvent(new UserUpdatedEvent()
        {
            Source = UserUpdatedEventSource.TrnToken,
            CreatedUtc = _clock.UtcNow,
            Changes = UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus,
            User = user,
            UpdatedByUserId = null,
            UpdatedByClientId = null
        });

        await _dbContext.SaveChangesAsync();

        return user;
    }
}
