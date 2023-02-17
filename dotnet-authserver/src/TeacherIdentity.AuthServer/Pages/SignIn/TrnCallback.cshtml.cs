using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.EmailVerified)]
public class TrnCallbackModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly IClock _clock;
    private readonly IUserVerificationService _userVerificationService;
    private readonly ILogger<TrnCallbackModel> _logger;

    public TrnCallbackModel(
        TeacherIdentityServerDbContext dbContext,
        IIdentityLinkGenerator linkGenerator,
        IClock clock,
        IUserVerificationService userVerificationService,
        ILogger<TrnCallbackModel> logger)
    {
        _dbContext = dbContext;
        _linkGenerator = linkGenerator;
        _clock = clock;
        _userVerificationService = userVerificationService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGet()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        var lookupState = await _dbContext.JourneyTrnLookupStates
            .Include(s => s.User)
            .SingleOrDefaultAsync(s => s.JourneyId == authenticationState.JourneyId);

        if (lookupState is null)
        {
            _logger.LogError("No TRN lookup state found for journey {JourneyId}.", authenticationState.JourneyId);
            return BadRequest();
        }

        Debug.Assert(lookupState.UserId is null);

        // We don't expect to have an existing user at this point
        if (authenticationState.UserId.HasValue)
        {
            throw new NotSupportedException();
        }

        var userId = Guid.NewGuid();
        var user = new User()
        {
            CompletedTrnLookup = _clock.UtcNow,
            Created = _clock.UtcNow,
            DateOfBirth = lookupState.DateOfBirth,
            EmailAddress = authenticationState.EmailAddress!,
            FirstName = string.IsNullOrEmpty(lookupState.PreferredFirstName) ? lookupState.OfficialFirstName : lookupState.PreferredFirstName,
            LastName = string.IsNullOrEmpty(lookupState.PreferredLastName) ? lookupState.OfficialLastName : lookupState.PreferredLastName,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            Trn = lookupState.Trn,
            TrnAssociationSource = !string.IsNullOrEmpty(lookupState.Trn) ? TrnAssociationSource.Lookup : null,
            LastSignedIn = _clock.UtcNow,
            RegisteredWithClientId = authenticationState.OAuthState?.ClientId,
            TrnLookupStatus = lookupState.Trn is not null ? TrnLookupStatus.Found : TrnLookupStatus.Pending
        };

        _dbContext.Users.Add(user);
        lookupState.Locked = _clock.UtcNow;
        lookupState.UserId = userId;

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

            var existingUser = await _dbContext.Users.SingleAsync(u => u.Trn == lookupState.Trn);
            var existingUserEmail = existingUser.EmailAddress;

            authenticationState.OnTrnLookupCompletedForTrnAlreadyInUse(existingUserEmail);

            var pinGenerationResult = await _userVerificationService.GenerateEmailPin(existingUserEmail);

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

            return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
        }

        authenticationState.OnTrnLookupCompletedAndUserRegistered(user);
        await authenticationState.SignIn(HttpContext);

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }
}
