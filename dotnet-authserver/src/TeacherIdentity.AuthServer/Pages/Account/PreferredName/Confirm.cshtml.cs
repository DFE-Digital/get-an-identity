using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Account.PreferredName;

[VerifyQueryParameterSignature]
public class ConfirmModel : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public ConfirmModel(
        IdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _linkGenerator = linkGenerator;
        _dbContext = dbContext;
        _clock = clock;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [FromQuery(Name = "preferredName")]
    public string? PreferredName { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        await UpdateUserName(User.GetUserId());
        return Redirect(_linkGenerator.Account(ClientRedirectInfo));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (PreferredName is null)
        {
            context.Result = BadRequest();
        }
    }

    private async Task UpdateUserName(Guid userId)
    {
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId);

        UserUpdatedEventChanges changes = UserUpdatedEventChanges.None;

        if (user.PreferredName != PreferredName)
        {
            changes |= UserUpdatedEventChanges.PreferredName;
        }

        if (changes != UserUpdatedEventChanges.None)
        {
            user.PreferredName = PreferredName!;
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

            TempData.SetFlashSuccess("Your preferred name has been updated");
        }
    }
}
