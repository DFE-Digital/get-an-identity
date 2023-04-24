using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[CheckJourneyType(typeof(LegacyTrnJourney))]
[CheckCanAccessStep(CurrentStep)]
public class TrnInUseChooseEmailModel : PageModel
{
    private const string CurrentStep = SignInJourney.Steps.TrnInUseChooseEmail;

    private readonly SignInJourney _journey;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public TrnInUseChooseEmailModel(
        SignInJourney journey,
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _journey = journey;
        _dbContext = dbContext;
        _clock = clock;
    }

    [BindProperty]
    [Display(Name = "Which email address do you want to use?")]
    [Required(ErrorMessage = "Enter the email address you want to use")]
    public string? Email { get; set; }

    public string SignedInEmail => _journey.AuthenticationState.EmailAddress!;

    public string ExistingAccountEmail => _journey.AuthenticationState.TrnOwnerEmailAddress!;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        // Ensure the email submitted is one of the two we have verified
        if (Email != SignedInEmail && Email != ExistingAccountEmail)
        {
            Email = null;
            return this.PageWithErrors();
        }

        var authenticationState = _journey.AuthenticationState;

        var lookupState = await _dbContext.JourneyTrnLookupStates
            .SingleOrDefaultAsync(s => s.JourneyId == authenticationState.JourneyId);

        var user = await _dbContext.Users.SingleAsync(u => u.EmailAddress == authenticationState.TrnOwnerEmailAddress);

        var emailChanged = user.EmailAddress != Email;

        user.EmailAddress = Email;

        if (lookupState is not null)
        {
            lookupState.Locked = _clock.UtcNow;
            lookupState.UserId = user.UserId;
        }

        if (emailChanged)
        {
            user.Updated = _clock.UtcNow;

            _dbContext.AddEvent(new Events.UserUpdatedEvent()
            {
                Source = Events.UserUpdatedEventSource.TrnMatchedToExistingUser,
                Changes = Events.UserUpdatedEventChanges.EmailAddress,
                CreatedUtc = _clock.UtcNow,
                User = user,
                UpdatedByUserId = user.UserId,
                UpdatedByClientId = null
            });
        }

        await _dbContext.SaveChangesAsync();

        authenticationState.OnEmailAddressChosen(user);
        await authenticationState.SignIn(HttpContext);

        return Redirect(_journey.GetNextStepUrl(CurrentStep));
    }
}
