using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
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

    public CreateUserHelper(
        TeacherIdentityServerDbContext dbContext,
        IUserVerificationService userVerificationService,
        IClock clock,
        IZendeskApiWrapper zendeskApiWrapper)
    {
        _dbContext = dbContext;
        _userVerificationService = userVerificationService;
        _clock = clock;
        _zendeskApiWrapper = zendeskApiWrapper;
    }

    public async Task<IActionResult> CreateOrMatchUserWithTrn(SignInJourney journey, string currentStep)
    {
        var authenticationState = journey.AuthenticationState;
        Debug.Assert(authenticationState.TrnLookupStatus.HasValue);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            CompletedTrnLookup = _clock.UtcNow,
            Created = _clock.UtcNow,
            DateOfBirth = authenticationState.DateOfBirth,
            EmailAddress = authenticationState.EmailAddress!,
            FirstName = authenticationState.FirstName ?? authenticationState.OfficialFirstName!,
            LastName = authenticationState.LastName ?? authenticationState.OfficialLastName!,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            Trn = authenticationState.Trn,
            TrnAssociationSource = authenticationState.Trn is not null ? TrnAssociationSource.Lookup : null,
            LastSignedIn = _clock.UtcNow,
            RegisteredWithClientId = authenticationState.OAuthState?.ClientId,
            TrnLookupStatus = authenticationState.TrnLookupStatus
        };

        _dbContext.Users.Add(user);

        _dbContext.AddEvent(new Events.UserRegisteredEvent()
        {
            ClientId = authenticationState.OAuthState?.ClientId,
            CreatedUtc = _clock.UtcNow,
            User = user
        });

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException dex) when (dex.IsUniqueIndexViolation("ix_users_trn"))
        {
            // TRN is already linked to an existing account

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

                throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReason)}: '{pinGenerationResult.FailedReason}'.");
            }

            return new RedirectResult(journey.GetNextStepUrl(currentStep));
        }

        authenticationState.OnTrnLookupCompletedAndUserRegistered(user);
        await authenticationState.SignIn(journey.HttpContext);

        if ((!authenticationState.TryGetOAuthState(out var oAuthState) || !oAuthState.HasScope(CustomScopes.Trn)) &&
            authenticationState.TrnLookupStatus == TrnLookupStatus.Pending)
        {
            await CreateTrnResolutionZendeskTicket(authenticationState);
        }

        return new RedirectResult(journey.GetNextStepUrl(currentStep));
    }

    private Task CreateTrnResolutionZendeskTicket(AuthenticationState authenticationState) =>
        _zendeskApiWrapper.CreateTicketAsync(new()
        {
            Subject = $"[Get an identity] - Support request from {authenticationState.GetPreferredName() ?? authenticationState.GetOfficialName()}",
            Comment = new TicketComment()
            {
                Body = $"""
                A user has submitted a request to find their TRN. Their information is:
                Name: {authenticationState.GetOfficialName()}
                Email: {authenticationState.EmailAddress}
                Previous name: {authenticationState.GetPreviousOfficialName() ?? "None"}
                Date of birth: {authenticationState.DateOfBirth:dd/MM/yyyy}
                NI number: {authenticationState.NationalInsuranceNumber ?? "Not provided"}
                ITT provider: {authenticationState.IttProviderName ?? "Not provided"}
                User-provided TRN: {authenticationState.StatedTrn ?? "Not provided"}
                """
            },
            Requester = new()
            {
                Email = authenticationState.EmailAddress,
                Name = authenticationState.GetPreferredName() ?? authenticationState.GetOfficialName()
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
}
