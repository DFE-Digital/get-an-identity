using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class ElevateUserTrnVerificationModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    public new User? User { get; set; }
    [FromRoute]
    public Guid UserId { get; set; }

    public ElevateUserTrnVerificationModel(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        User!.TrnVerificationLevel = null;
        User!.TrnAssociationSource = TrnAssociationSource.SupportUi;
        _dbContext.AddEvent(new Events.UserUpdatedEvent
        {
            Source = Events.UserUpdatedEventSource.SupportUi,
            CreatedUtc = _clock.UtcNow,
            Changes = Events.UserUpdatedEventChanges.TrnVerificationLevel | Events.UserUpdatedEventChanges.TrnAssociationSource,
            User = User,
            UpdatedByUserId = HttpContext.User.GetUserId(),
            UpdatedByClientId = null
        });
        await _dbContext.SaveChangesAsync();

        TempData.SetFlashSuccess("TRN verification level elevated");

        return RedirectToPage("/Admin/User", new { UserId });
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        User = await GetUser(UserId);

        if (User == null)
        {
            context.Result = NotFound();
            return;
        }

        if (User.EffectiveVerificationLevel != TrnVerificationLevel.Low)
        {
            context.Result = BadRequest();
            return;
        }

        await next();
    }

    private async Task<User?> GetUser(Guid userId)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == userId);

        return (user is null || user.UserType != UserType.Default) ? null : user;
    }
}
