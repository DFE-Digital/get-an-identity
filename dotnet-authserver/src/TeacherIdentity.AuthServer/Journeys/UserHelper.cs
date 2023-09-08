using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Services.Zendesk;
using ZendeskApi.Client.Models;

namespace TeacherIdentity.AuthServer.Journeys;

public class UserHelper
{
    private const string DebugLogsContainerName = "debug-logs";

    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IUserVerificationService _userVerificationService;
    private readonly IClock _clock;
    private readonly IZendeskApiWrapper _zendeskApiWrapper;
    private readonly TrnTokenHelper _trnTokenHelper;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly BlobServiceClient _blobServiceClient;

    public UserHelper(
        TeacherIdentityServerDbContext dbContext,
        IUserVerificationService userVerificationService,
        IClock clock,
        IZendeskApiWrapper zendeskApiWrapper,
        TrnTokenHelper trnTokenHelper,
        IDqtApiClient dqtApiClient,
        BlobServiceClient blobServiceClient)
    {
        _dbContext = dbContext;
        _userVerificationService = userVerificationService;
        _clock = clock;
        _zendeskApiWrapper = zendeskApiWrapper;
        _trnTokenHelper = trnTokenHelper;
        _dqtApiClient = dqtApiClient;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<User> CreateUser(AuthenticationState authenticationState)
    {
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Created = _clock.UtcNow,
            DateOfBirth = authenticationState.DateOfBirth,
            EmailAddress = authenticationState.EmailAddress!,
            MobileNumber = authenticationState.MobileNumber,
            NormalizedMobileNumber = MobileNumber.Parse(authenticationState.MobileNumber!),
            FirstName = authenticationState.FirstName!,
            MiddleName = authenticationState.MiddleName,
            LastName = authenticationState.LastName!,
            PreferredName = authenticationState.PreferredName,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            LastSignedIn = _clock.UtcNow,
            RegisteredWithClientId = authenticationState.OAuthState?.ClientId,
            NationalInsuranceNumber = authenticationState.NationalInsuranceNumber,
        };

        _dbContext.Users.Add(user);

        _dbContext.AddEvent(new Events.UserRegisteredEvent()
        {
            ClientId = authenticationState.OAuthState?.ClientId,
            CreatedUtc = _clock.UtcNow,
            User = user
        });

        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<User> CreateUserWithTrnLookup(AuthenticationState authenticationState)
    {
        var userId = Guid.NewGuid();
        var useDqtRecordForNames = authenticationState.Trn is not null
            && !string.IsNullOrEmpty(authenticationState.DqtFirstName)
            && !string.IsNullOrEmpty(authenticationState.DqtLastName);

        var user = new User()
        {
            CompletedTrnLookup = _clock.UtcNow,
            Created = _clock.UtcNow,
            DateOfBirth = authenticationState.DateOfBirth,
            EmailAddress = authenticationState.EmailAddress!,
            MobileNumber = authenticationState.MobileNumber,
            FirstName = useDqtRecordForNames ? authenticationState.DqtFirstName! : authenticationState.FirstName!,
            MiddleName = useDqtRecordForNames ? authenticationState.DqtMiddleName : authenticationState.MiddleName,
            LastName = useDqtRecordForNames ? authenticationState.DqtLastName! : authenticationState.LastName!,
            PreferredName = authenticationState.PreferredName,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            Trn = authenticationState.Trn,
            TrnAssociationSource = authenticationState.Trn is null ? null : TrnAssociationSource.Lookup,
            LastSignedIn = _clock.UtcNow,
            RegisteredWithClientId = authenticationState.OAuthState?.ClientId,
            TrnLookupStatus = authenticationState.TrnLookupStatus,
            TrnVerificationLevel = TrnVerificationLevel.Low,
            NationalInsuranceNumber = authenticationState.NationalInsuranceNumber,
        };

        _dbContext.Users.Add(user);

        if (authenticationState.HasTrnToken)
        {
            await _trnTokenHelper.InvalidateTrnToken(authenticationState.TrnToken!, user.UserId);
        }

        _dbContext.AddEvent(new Events.UserRegisteredEvent()
        {
            ClientId = authenticationState.OAuthState?.ClientId,
            CreatedUtc = _clock.UtcNow,
            User = user
        });

        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<User> CreateUserWithTrnToken(AuthenticationState authenticationState)
    {
        var userId = Guid.NewGuid();

        var user = new User()
        {
            CompletedTrnLookup = null,
            Created = _clock.UtcNow,
            DateOfBirth = authenticationState.DateOfBirth,
            EmailAddress = authenticationState.EmailAddress!,
            MobileNumber = authenticationState.MobileNumber,
            FirstName = authenticationState.FirstName!,
            MiddleName = authenticationState.MiddleName,
            LastName = authenticationState.LastName!,
            PreferredName = authenticationState.PreferredName,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            Trn = authenticationState.Trn,
            TrnAssociationSource = TrnAssociationSource.TrnToken,
            LastSignedIn = _clock.UtcNow,
            RegisteredWithClientId = authenticationState.OAuthState?.ClientId,
            TrnLookupStatus = authenticationState.TrnLookupStatus,
            NationalInsuranceNumber = authenticationState.NationalInsuranceNumber,
        };

        _dbContext.Users.Add(user);

        await _trnTokenHelper.InvalidateTrnToken(authenticationState.TrnToken!, user.UserId);

        _dbContext.AddEvent(new Events.UserRegisteredEvent()
        {
            ClientId = authenticationState.OAuthState?.ClientId,
            CreatedUtc = _clock.UtcNow,
            User = user
        });

        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<IActionResult> GeneratePinForExistingUserAccount(SignInJourney journey, string currentStep)
    {
        var authenticationState = journey.AuthenticationState;
        var existingUser = await _dbContext.Users.SingleAsync(u => u.Trn == authenticationState.Trn);
        var existingUserEmail = existingUser.EmailAddress;

        authenticationState.OnTrnLookupCompletedForTrnAlreadyInUse(existingUserEmail);

        var pinGenerationResult = await _userVerificationService.GenerateEmailPin(existingUserEmail);

        if (pinGenerationResult.FailedReason != PinGenerationFailedReason.None)
        {
            if (pinGenerationResult.FailedReason == PinGenerationFailedReason.RateLimitExceeded)
            {
                return new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyRequests"
                };
            }

            throw new NotImplementedException(
                $"Unknown {nameof(PinGenerationFailedReason)}: '{pinGenerationResult.FailedReason}'.");
        }

        return new RedirectResult(journey.GetNextStepUrl(currentStep));
    }

    public async Task CreateTrnResolutionZendeskTicket(
        Guid userId,
        string? officialName,
        string? preferredName,
        string? emailAddress,
        DateOnly? dateOfBirth,
        string? nationalInsuranceNumber,
        string? ittProviderName,
        string? statedTrn,
        string? serviceName,
        bool requiresTrnLookup)
    {
        var contactUserWithTrn = requiresTrnLookup ? "Yes" : "No";
        var ticketComment = $"""
                A user has submitted a request to find their TRN. Their information is:
                Name: {officialName}
                Email: {emailAddress}
                Date of birth: {dateOfBirth:dd/MM/yyyy}
                NI number: {nationalInsuranceNumber ?? "Not provided"}
                ITT provider: {ittProviderName ?? "Not provided"}
                User-provided TRN: {statedTrn ?? "Not provided"}
                Service: {serviceName ?? "Not provided"}
                Contact user with TRN: {contactUserWithTrn}
                """;

        var ticketResponse = await _zendeskApiWrapper.CreateTicketAsync(new()
        {
            Subject = $"[Get an identity] - Support request from {preferredName ?? officialName}",
            Comment = new TicketComment()
            {
                Body = ticketComment
            },
            Requester = new()
            {
                Email = emailAddress,
                Name = preferredName ?? officialName
            },
            CustomFields = new CustomFields()
            {
                new()
                {
                    Id = 4419328659089,
                    Value = "request_from_identity_auth_service"
                }
            }
        });

        var user = await _dbContext.Users.Where(u => u.UserId == userId).SingleAsync();
        user.TrnLookupSupportTicketCreated = true;
        _dbContext.AddEvent(new Events.TrnLookupSupportTicketCreatedEvent()
        {
            TicketId = ticketResponse.Ticket.Id,
            TicketComment = ticketComment,
            UserId = user.UserId,
            CreatedUtc = _clock.UtcNow,
        });

        await _dbContext.SaveChangesAsync();
    }

    public async Task EnsureDqtUserNameMatch(User user, AuthenticationState authenticationState)
    {
        var dqtUser = await _dqtApiClient.GetTeacherByTrn(user.Trn!);

        if (await CheckDqtTeacherRecordIsValid(dqtUser) &&
            NameHelper.GetFullName(user.FirstName, user.MiddleName, user.LastName) !=
            NameHelper.GetFullName(dqtUser!.FirstName, dqtUser.MiddleName, dqtUser.LastName))
        {
            await AssignDqtUserName(user.UserId, dqtUser);
            authenticationState.OnNameSet(dqtUser.FirstName, dqtUser.MiddleName, dqtUser.LastName);
        }
    }

    private async Task AssignDqtUserName(Guid userId, TeacherInfo dqtUser)
    {
        var existingUser = await _dbContext.Users.SingleAsync(u => u.UserId == userId);

        var changes = (existingUser.FirstName != dqtUser.FirstName ? Events.UserUpdatedEventChanges.FirstName : Events.UserUpdatedEventChanges.None) |
                      ((existingUser.MiddleName ?? string.Empty) != dqtUser.MiddleName ? Events.UserUpdatedEventChanges.MiddleName : Events.UserUpdatedEventChanges.None) |
                      (existingUser.LastName != dqtUser.LastName ? Events.UserUpdatedEventChanges.LastName : Events.UserUpdatedEventChanges.None);

        existingUser.FirstName = dqtUser.FirstName;
        existingUser.MiddleName = dqtUser.MiddleName;
        existingUser.LastName = dqtUser.LastName;

        _dbContext.AddEvent(new Events.UserUpdatedEvent()
        {
            Source = Events.UserUpdatedEventSource.DqtSynchronization,
            CreatedUtc = _clock.UtcNow,
            Changes = changes,
            User = Events.User.FromModel(existingUser),
            UpdatedByUserId = null,
            UpdatedByClientId = null
        });

        await _dbContext.SaveChangesAsync();
    }

    private async Task<bool> CheckDqtTeacherRecordIsValid(TeacherInfo? teacher)
    {
        if (teacher is null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(teacher.FirstName) || string.IsNullOrEmpty(teacher.LastName))
        {
            try
            {
                var blobName = $"{nameof(UserHelper)}-{_clock.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid()}.json";
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
