using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Account.Name;

public class Confirm : PageModel
{
    private TeacherIdentityServerDbContext _dbContext;
    private IClock _clock;

    public Confirm(
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [FromQuery(Name = "firstName")]
    public ProtectedString? FirstName { get; set; }

    [FromQuery(Name = "lastName")]
    public ProtectedString? LastName { get; set; }

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }
    public string? SafeReturnUrl { get; set; }


    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        await UpdateUser(User.GetUserId()!.Value);
        return Redirect(SafeReturnUrl!);
    }

    private async Task UpdateUser(Guid userId)
    {
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId);

        var newFirstName = FirstName!.PlainValue;
        var newLastName = LastName!.PlainValue;

        UserUpdatedEventChanges changes = UserUpdatedEventChanges.None;

        if (user.FirstName != newFirstName)
        {
            changes |= UserUpdatedEventChanges.FirstName;
        }

        if (user.LastName != newLastName)
        {
            changes |= UserUpdatedEventChanges.LastName;
        }

        if (changes != UserUpdatedEventChanges.None)
        {
            user.FirstName = newFirstName;
            user.LastName = newLastName;
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

            TempData.SetFlashSuccess("Your name has been updated");
        }
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (FirstName is null || LastName is null)
        {
            context.Result = new BadRequestResult();
            return;
        }

        SafeReturnUrl = !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/account";
    }
}
