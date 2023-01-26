using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

public class TrnCreateUserPageModel : PageModel
{
    protected readonly IIdentityLinkGenerator LinkGenerator;

    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IEmailVerificationService _emailVerificationService;

    public TrnCreateUserPageModel(
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IEmailVerificationService emailVerificationService)
    {
        LinkGenerator = linkGenerator;
        _dbContext = dbContext;
        _clock = clock;
        _emailVerificationService = emailVerificationService;
    }

    protected async Task<IActionResult> TryCreateUser()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        var userId = Guid.NewGuid();
        var user = new User()
        {
            CompletedTrnLookup = _clock.UtcNow,
            Created = _clock.UtcNow,
            DateOfBirth = authenticationState.DateOfBirth,
            EmailAddress = authenticationState.EmailAddress!,
            FirstName = string.IsNullOrEmpty(authenticationState.FirstName) ? authenticationState.OfficialFirstName! : authenticationState.FirstName,
            LastName = string.IsNullOrEmpty(authenticationState.LastName) ? authenticationState.OfficialLastName! : authenticationState.LastName,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            Trn = authenticationState.Trn,
            TrnAssociationSource = !string.IsNullOrEmpty(authenticationState.Trn) ? TrnAssociationSource.Lookup : null,
            LastSignedIn = _clock.UtcNow,
            RegisteredWithClientId = authenticationState.OAuthState?.ClientId,
            TrnLookupStatus = !string.IsNullOrEmpty(authenticationState.Trn) ? TrnLookupStatus.Found : TrnLookupStatus.None
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

        return Redirect(authenticationState.GetNextHopUrl(LinkGenerator));
    }
}
