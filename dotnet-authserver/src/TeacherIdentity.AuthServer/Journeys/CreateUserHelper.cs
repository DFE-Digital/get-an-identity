using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Services.Zendesk;
using ZendeskApi.Client.Models;

namespace TeacherIdentity.AuthServer.Journeys;

public class CreateUserHelper
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IUserVerificationService _userVerificationService;
    private readonly IClock _clock;
    private readonly IZendeskApiWrapper _zendeskApiWrapper;
    private readonly TrnTokenHelper _trnTokenHelper;

    public CreateUserHelper(
        TeacherIdentityServerDbContext dbContext,
        IUserVerificationService userVerificationService,
        IClock clock,
        IZendeskApiWrapper zendeskApiWrapper,
        TrnTokenHelper trnTokenHelper)
    {
        _dbContext = dbContext;
        _userVerificationService = userVerificationService;
        _clock = clock;
        _zendeskApiWrapper = zendeskApiWrapper;
        _trnTokenHelper = trnTokenHelper;
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
            LastName = authenticationState.LastName!,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            LastSignedIn = _clock.UtcNow,
            RegisteredWithClientId = authenticationState.OAuthState?.ClientId,
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

        var user = new User()
        {
            CompletedTrnLookup = _clock.UtcNow,
            Created = _clock.UtcNow,
            DateOfBirth = authenticationState.DateOfBirth,
            EmailAddress = authenticationState.EmailAddress!,
            MobileNumber = authenticationState.MobileNumber,
            FirstName = authenticationState.FirstName ?? authenticationState.OfficialFirstName!,
            LastName = authenticationState.LastName ?? authenticationState.OfficialLastName!,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            Trn = authenticationState.Trn,
            TrnAssociationSource = authenticationState.Trn is null ? null : TrnAssociationSource.Lookup,
            LastSignedIn = _clock.UtcNow,
            RegisteredWithClientId = authenticationState.OAuthState?.ClientId,
            TrnLookupStatus = authenticationState.TrnLookupStatus
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
            FirstName = authenticationState.FirstName ?? authenticationState.OfficialFirstName!,
            LastName = authenticationState.LastName ?? authenticationState.OfficialLastName!,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            Trn = authenticationState.Trn,
            TrnAssociationSource = TrnAssociationSource.TrnToken,
            LastSignedIn = _clock.UtcNow,
            RegisteredWithClientId = authenticationState.OAuthState?.ClientId,
            TrnLookupStatus = authenticationState.TrnLookupStatus,
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
        string? previousOfficialName,
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
                Previous name: {previousOfficialName ?? "None"}
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
}
