using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;
using TeacherIdentity.AuthServer.Services.Zendesk;
using ZendeskApi.Client.Models;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

public class TrnCreateUserPageModel : PageModel
{
    protected readonly IIdentityLinkGenerator LinkGenerator;

    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IZendeskApiWrapper _zendeskApiWrapper;

    public TrnCreateUserPageModel(
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IEmailVerificationService emailVerificationService,
        IZendeskApiWrapper zendeskApiWrapper)
    {
        LinkGenerator = linkGenerator;
        _dbContext = dbContext;
        _clock = clock;
        _emailVerificationService = emailVerificationService;
        _zendeskApiWrapper = zendeskApiWrapper;
    }

    protected async Task<IActionResult> TryCreateUser()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

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

            var pinGenerationResult = await _emailVerificationService.GeneratePin(existingUserEmail);

            if (pinGenerationResult.FailedReasons != PinGenerationFailedReasons.None)
            {
                if (pinGenerationResult.FailedReasons == PinGenerationFailedReasons.RateLimitExceeded)
                {
                    return new ViewResult()
                    {
                        StatusCode = 429,
                        ViewName = "TooManyRequests"
                    };
                }

                throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReasons)}: '{pinGenerationResult.FailedReasons}'.");
            }

            return Redirect(authenticationState.GetNextHopUrl(LinkGenerator));
        }

        authenticationState.OnTrnLookupCompletedAndUserRegistered(user);
        await authenticationState.SignIn(HttpContext);

        if (authenticationState.TrnLookupStatus == TrnLookupStatus.Pending)
        {
            await CreateTrnResolutionZendeskTicket(authenticationState);
        }

        return Redirect(authenticationState.GetNextHopUrl(LinkGenerator));
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
