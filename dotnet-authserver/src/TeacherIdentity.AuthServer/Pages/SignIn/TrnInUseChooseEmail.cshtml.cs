using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class TrnInUseChooseEmailModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public TrnInUseChooseEmailModel(
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IIdentityLinkGenerator linkGenerator)
    {
        _dbContext = dbContext;
        _clock = clock;
        _linkGenerator = linkGenerator;
    }

    [BindProperty]
    [Required(ErrorMessage = "Enter the email address you want to use")]
    public string? Email { get; set; }

    public string SignedInEmail => HttpContext.GetAuthenticationState().EmailAddress!;

    public string ExistingAccountEmail => HttpContext.GetAuthenticationState().TrnOwnerEmailAddress!;

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

        var authenticationState = HttpContext.GetAuthenticationState();

        var lookupState = await _dbContext.JourneyTrnLookupStates
            .SingleAsync(s => s.JourneyId == authenticationState.JourneyId);
        var user = await _dbContext.Users.SingleAsync(u => u.EmailAddress == authenticationState.TrnOwnerEmailAddress);

        user.EmailAddress = Email;
        lookupState.Locked = _clock.UtcNow;
        lookupState.UserId = user.UserId;

        await _dbContext.SaveChangesAsync();

        authenticationState.OnEmailAddressChosen(user);
        await authenticationState.SignIn(HttpContext);

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        if (string.IsNullOrEmpty(authenticationState.EmailAddress) ||
            !authenticationState.EmailAddressVerified ||
            authenticationState.TrnLookup != AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified)
        {
            context.Result = Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
        }
    }
}
