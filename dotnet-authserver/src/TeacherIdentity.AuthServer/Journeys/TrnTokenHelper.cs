using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
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
    private const string DebugLogsContainerName = "debug-logs";

    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IConfiguration _configuration;
    private readonly IUserSearchService _userSearchService;
    private readonly BlobServiceClient _blobServiceClient;

    public TrnTokenHelper(
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IDqtApiClient dqtApiClient,
        IConfiguration configuration,
        IUserSearchService userSearchService,
        BlobServiceClient blobServiceClient)
    {
        _dbContext = dbContext;
        _clock = clock;
        _dqtApiClient = dqtApiClient;
        _configuration = configuration;
        _userSearchService = userSearchService;
        _blobServiceClient = blobServiceClient;
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

        if (user.Trn is null || user.Trn == trnToken.Trn)
        {
            user = await UpdateUserFromToken(user, trnToken.Trn);
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

        await CheckDqtTeacherNames(teacher);

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
            DateOfBirth = teacher.DateOfBirth
        };
    }

    private async Task<User> UpdateUserFromToken(User user, string trn)
    {
        var changes = UserUpdatedEventChanges.None;

        var dqtUser = await _dqtApiClient.GetTeacherByTrn(trn);
        if (dqtUser is not null && await CheckDqtTeacherNames(dqtUser))
        {
            changes |= (user.FirstName != dqtUser.FirstName ? UserUpdatedEventChanges.FirstName : UserUpdatedEventChanges.None) |
                       ((user.MiddleName ?? string.Empty) != dqtUser.MiddleName ? UserUpdatedEventChanges.MiddleName : UserUpdatedEventChanges.None) |
                       (user.LastName != dqtUser.LastName ? UserUpdatedEventChanges.LastName : UserUpdatedEventChanges.None);

            user.FirstName = dqtUser.FirstName;
            user.MiddleName = dqtUser.MiddleName;
            user.LastName = dqtUser.LastName;
        }

        if (user.Trn is null)
        {
            user.Trn = trn;
            user.TrnLookupStatus = TrnLookupStatus.Found;
            user.TrnAssociationSource = TrnAssociationSource.TrnToken;

            changes |= UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus;
        }

        if (changes != UserUpdatedEventChanges.None)
        {
            user.Updated = _clock.UtcNow;

            _dbContext.AddEvent(new UserUpdatedEvent()
            {
                Source = UserUpdatedEventSource.TrnToken,
                CreatedUtc = _clock.UtcNow,
                Changes = changes,
                User = user,
                UpdatedByUserId = null,
                UpdatedByClientId = null
            });

            await _dbContext.SaveChangesAsync();
        }

        return user;
    }

    private async Task<bool> CheckDqtTeacherNames(TeacherInfo teacher)
    {
        if (string.IsNullOrEmpty(teacher.FirstName) || string.IsNullOrEmpty(teacher.LastName))
        {
            try
            {
                var blobName = $"{nameof(TrnTokenHelper)}-{_clock.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid()}.json";
                var blobClient = await GetBlobClient(blobName);
                var debugLog = JsonSerializer.Serialize(teacher);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(debugLog));
                await blobClient.UploadAsync(stream);
            }
            catch (Exception)
            {
                // Don't want logging issues to abort whole process
            }

            return false;
        }

        return true;
    }

    private async Task<BlobClient> GetBlobClient(string blobName)
    {
        var blobContainerClient = await GetBlobContainerClient();
        return blobContainerClient.GetBlobClient(blobName);
    }

    private async Task<BlobContainerClient> GetBlobContainerClient()
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(DebugLogsContainerName);
        await blobContainerClient.CreateIfNotExistsAsync();
        return blobContainerClient;
    }
}
