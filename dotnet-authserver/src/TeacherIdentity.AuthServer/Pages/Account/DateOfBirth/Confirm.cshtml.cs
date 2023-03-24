using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Account.DateOfBirth;

public class Confirm : PageModel
{
    private TeacherIdentityServerDbContext _dbContext;
    private IClock _clock;

    public Confirm(
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [FromQuery(Name = "dateOfBirth")]
    public ProtectedString? DateOfBirth { get; set; }
    public DateOnly NewDateOfBirth { get; set; }

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }
    public string? SafeReturnUrl { get; set; }


    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        await UpdateUserDateOfBirth(User.GetUserId()!.Value);
        return Redirect(SafeReturnUrl!);
    }

    private async Task UpdateUserDateOfBirth(Guid userId)
    {
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId);

        UserUpdatedEventChanges changes = UserUpdatedEventChanges.None;

        if (user.DateOfBirth != NewDateOfBirth)
        {
            changes |= UserUpdatedEventChanges.DateOfBirth;
        }

        if (changes != UserUpdatedEventChanges.None)
        {
            user.DateOfBirth = NewDateOfBirth;
            user.Updated = _clock.UtcNow;

            _dbContext.AddEvent(new UserUpdatedEvent()
            {
                Source = UserUpdatedEventSource.ChangedByUser,
                CreatedUtc = _clock.UtcNow,
                Changes = changes,
                User = user,
                UpdatedByUserId = User.GetUserId()!.Value,
                UpdatedByClientId = null
            });

            await _dbContext.SaveChangesAsync();

            await HttpContext.SignInCookies(user, resetIssued: false);

            TempData.SetFlashSuccess("Your date of birth has been updated");
        }
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (DateOfBirth is null || !DateOnly.TryParse(DateOfBirth.PlainValue, out var newDateOfBirth))
        {
            context.Result = new BadRequestResult();
            return;
        }

        NewDateOfBirth = newDateOfBirth;
        SafeReturnUrl = !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/account";
    }
}
