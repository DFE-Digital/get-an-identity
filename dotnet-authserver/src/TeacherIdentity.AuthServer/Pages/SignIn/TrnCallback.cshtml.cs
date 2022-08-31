using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class TrnCallbackModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IClock _clock;
    private readonly ILogger<TrnCallbackModel> _logger;

    public TrnCallbackModel(
        TeacherIdentityServerDbContext dbContext,
        IDqtApiClient apiClient,
        IClock clock,
        ILogger<TrnCallbackModel> logger)
    {
        _dbContext = dbContext;
        _dqtApiClient = apiClient;
        _clock = clock;
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
                DateOfBirth = lookupState.DateOfBirth,
                EmailAddress = authenticationState.EmailAddress!,
                FirstName = lookupState.FirstName,
                LastName = lookupState.LastName,
                UserId = userId
            };

            _dbContext.Users.Add(user);
            lookupState.Locked = _clock.UtcNow;
            lookupState.UserId = userId;

            await _dbContext.SaveChangesAsync();
        }

        var trn = lookupState.Trn;
        if (!string.IsNullOrEmpty(trn))
        {
            await _dqtApiClient.SetTeacherIdentityInfo(new DqtTeacherIdentityInfo() { Trn = trn!, UserId = user.UserId });
        }

        await HttpContext.SignInUser(user, firstTimeUser: true, trn);

        return Redirect(authenticationState.GetNextHopUrl(Url));
    }
}
