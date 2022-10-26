using System.ComponentModel.DataAnnotations;
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

    public UpdateNameModel(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [BindProperty]
    [Display(Name = "Preferred first name")]
    [Required(ErrorMessage = "Enter your preferred first name")]
    [MaxLength(200, ErrorMessage = "Preferred first name must be 200 characters or less")]
    public string? FirstName { get; set; }

    [BindProperty]
    [Display(Name = "Preferred last name")]
    [Required(ErrorMessage = "Enter your preferred last name")]
    [MaxLength(200, ErrorMessage = "Preferred last name must be 200 characters or less")]
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
        }

        if (HttpContext.TryGetAuthenticationState(out var authenticationState))
        {
            authenticationState.OnNameChanged(FirstName!, LastName!);
        }

        TempData.SetFlashSuccess("Preferred name updated");

        var safeReturnUrl = !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ?
            ReturnUrl :
            "/";

        return Redirect(safeReturnUrl);
    }
}
