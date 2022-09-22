using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.BackgroundJobs;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class TrnCallbackModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly IClock _clock;
    private readonly IBackgroundJobScheduler _backgroundJobScheduler;
    private readonly ILogger<TrnCallbackModel> _logger;

    public TrnCallbackModel(
        TeacherIdentityServerDbContext dbContext,
        IIdentityLinkGenerator linkGenerator,
        IClock clock,
        IBackgroundJobScheduler backgroundJobScheduler,
        ILogger<TrnCallbackModel> logger)
    {
        _dbContext = dbContext;
        _linkGenerator = linkGenerator;
        _clock = clock;
        _backgroundJobScheduler = backgroundJobScheduler;
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

        // We don't expect to have an existing user at this point
        if (authenticationState.UserId.HasValue)
        {
            throw new NotSupportedException();
        }

        User user;

        if (lookupState.Locked.HasValue)
        {
            // User has already been registered
            Debug.Assert(lookupState.UserId.HasValue);
            user = lookupState.User!;
        }
        else
        {
            var userId = Guid.NewGuid();
            user = new User()
            {
                Created = _clock.UtcNow,
                DateOfBirth = lookupState.DateOfBirth,
                EmailAddress = authenticationState.EmailAddress!,
                FirstName = lookupState.FirstName,
                LastName = lookupState.LastName,
                UserId = userId,
                UserType = UserType.Default,
                Trn = lookupState.Trn,
                CompletedTrnLookup = _clock.UtcNow
            };

            _dbContext.Users.Add(user);
            lookupState.Locked = _clock.UtcNow;
            lookupState.UserId = userId;

            await _dbContext.SaveChangesAsync();
        }

        var trn = lookupState.Trn;
        if (!string.IsNullOrEmpty(trn))
        {
            await _backgroundJobScheduler.Enqueue<IDqtApiClient>(
                dqtApiClient => dqtApiClient.SetTeacherIdentityInfo(new DqtTeacherIdentityInfo() { Trn = trn!, UserId = user.UserId }));
        }

        authenticationState.OnTrnLookupCompleted(user, firstTimeUser: true);
        await HttpContext.SignInUserFromAuthenticationState();

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }
}
