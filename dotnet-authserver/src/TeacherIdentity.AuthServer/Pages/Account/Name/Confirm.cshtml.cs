using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Account.Name;

[VerifyQueryParameterSignature]
[CheckNameChangeIsEnabled]
public class Confirm : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly IClock _clock;

    public Confirm(
        IdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _linkGenerator = linkGenerator;
        _dbContext = dbContext;
        _clock = clock;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [FromQuery(Name = "firstName")]
    public string? FirstName { get; set; }

    [FromQuery(Name = "middleName")]
    public string? MiddleName { get; set; }

    [FromQuery(Name = "lastName")]
    public string? LastName { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        await UpdateUserName(User.GetUserId());
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

        if (user.MiddleName != MiddleName)
        {
            changes |= UserUpdatedEventChanges.MiddleName;
        }

        if (user.LastName != LastName)
        {
            changes |= UserUpdatedEventChanges.LastName;
        }

        if (changes != UserUpdatedEventChanges.None)
        {
            user.FirstName = FirstName!;
            user.LastName = LastName!;
            user.MiddleName = MiddleName;
            user.Updated = _clock.UtcNow;

            _dbContext.AddEvent(new UserUpdatedEvent()
            {
                Source = UserUpdatedEventSource.ChangedByUser,
                CreatedUtc = _clock.UtcNow,
                Changes = changes,
                User = user,
                UpdatedByUserId = User.GetUserId(),
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
