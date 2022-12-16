using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Authenticated;

public class UpdateNameModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public UpdateNameModel(
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IIdentityLinkGenerator linkGenerator)
    {
        _dbContext = dbContext;
        _clock = clock;
        _linkGenerator = linkGenerator;
    }

    [BindProperty]
    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter your first name")]
    [MaxLength(200, ErrorMessage = "First name must be 200 characters or less")]
    public string? FirstName { get; set; }

    [BindProperty]
    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter your last name")]
    [MaxLength(200, ErrorMessage = "Last name must be 200 characters or less")]
    public string? LastName { get; set; }

    [FromQuery(Name = "cancelUrl")]
    public string? CancelUrl { get; set; }

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    public async Task OnGet()
    {
        var userId = User.GetUserId()!.Value;
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId);

        FirstName = user.FirstName;
        LastName = user.LastName;
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var userId = User.GetUserId()!.Value;
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId);

        UserUpdatedEventChanges changes = UserUpdatedEventChanges.None;

        if (user.FirstName != FirstName)
        {
            changes |= UserUpdatedEventChanges.FirstName;
        }

        if (user.LastName != LastName)
        {
            changes |= UserUpdatedEventChanges.LastName;
        }

        user.FirstName = FirstName!;
        user.LastName = LastName!;

        var safeReturnUrl = !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ?
            ReturnUrl :
            "/";

        if (changes != UserUpdatedEventChanges.None)
        {
            user.Updated = _clock.UtcNow;

            _dbContext.AddEvent(new Events.UserUpdatedEvent()
            {
                Source = Events.UserUpdatedEventSource.ChangedByUser,
                CreatedUtc = _clock.UtcNow,
                Changes = changes,
                User = Events.User.FromModel(user)
            });

            await _dbContext.SaveChangesAsync();

            await HttpContext.SignInCookies(user, resetIssued: false);

            if (HttpContext.TryGetAuthenticationState(out var authenticationState))
            {
                authenticationState.OnNameChanged(FirstName!, LastName!);

                // If we're inside an OAuth journey we need to redirect back to the authorize endpoint so the
                // OpenIddict auth handler can SignIn again with the revised user details

                authenticationState.EnsureOAuthState();
                Debug.Assert(ReturnUrl == _linkGenerator.CompleteAuthorization());

                safeReturnUrl = authenticationState.PostSignInUrl;
            }

            TempData.SetFlashSuccess("Name updated");
        }

        return Redirect(safeReturnUrl);
    }
}
