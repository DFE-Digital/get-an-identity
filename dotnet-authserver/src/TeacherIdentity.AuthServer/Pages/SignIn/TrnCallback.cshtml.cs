using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.BackgroundJobs;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class TrnCallbackModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly IClock _clock;
    private readonly IBackgroundJobScheduler _backgroundJobScheduler;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ILogger<TrnCallbackModel> _logger;

    public TrnCallbackModel(
        TeacherIdentityServerDbContext dbContext,
        IIdentityLinkGenerator linkGenerator,
        IClock clock,
        IBackgroundJobScheduler backgroundJobScheduler,
        IEmailVerificationService emailVerificationService,
        ILogger<TrnCallbackModel> logger)
    {
        _dbContext = dbContext;
        _linkGenerator = linkGenerator;
        _clock = clock;
        _backgroundJobScheduler = backgroundJobScheduler;
        _emailVerificationService = emailVerificationService;
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
            FirstName = lookupState.FirstName,
            LastName = lookupState.LastName,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            Trn = lookupState.Trn,
            LastSignedIn = _clock.UtcNow
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

            await _emailVerificationService.GeneratePin(existingUserEmail);

            return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
        }

        var trn = lookupState.Trn;
        if (!string.IsNullOrEmpty(trn))
        {
            await _backgroundJobScheduler.Enqueue<IDqtApiClient>(
                dqtApiClient => dqtApiClient.SetTeacherIdentityInfo(new DqtTeacherIdentityInfo() { Trn = trn!, UserId = user.UserId }));
        }

        authenticationState.OnTrnLookupCompletedAndUserRegistered(user, firstTimeSignInForEmail: true);
        await authenticationState.SignIn(HttpContext);

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        if (authenticationState.TrnLookup != AuthenticationState.TrnLookupState.None)
        {
            context.Result = Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
        }
    }
}
