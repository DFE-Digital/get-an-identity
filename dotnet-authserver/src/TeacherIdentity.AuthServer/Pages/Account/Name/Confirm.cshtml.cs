using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Account.Name;

public class Confirm : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly IClock _clock;

    public Confirm(
        TeacherIdentityServerDbContext dbContext,
        IdentityLinkGenerator linkGenerator,
        IClock clock)
    {
        _dbContext = dbContext;
        _linkGenerator = linkGenerator;
        _clock = clock;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [FromQuery(Name = "firstName")]
    [VerifyInSignature]
    public string? FirstName { get; set; }

    [FromQuery(Name = "lastName")]
    [VerifyInSignature]
    public string? LastName { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        await UpdateUserName(User.GetUserId()!.Value);
        return Redirect(_linkGenerator.Account(ClientRedirectInfo));
    }

    private async Task UpdateUserName(Guid userId)
    {
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

        if (changes != UserUpdatedEventChanges.None)
        {
            user.FirstName = FirstName!;
            user.LastName = LastName!;
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
            context.Result = BadRequest();
        }
    }
}
