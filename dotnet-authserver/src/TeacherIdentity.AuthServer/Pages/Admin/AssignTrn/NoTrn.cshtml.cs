using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin.AssignTrn;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class NoTrn : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public NoTrn(
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [BindProperty]
    [Display(Name = "Confirm user is not in DQT")]
    public bool? HasNoTrn { get; set; }

    [FromRoute]
    public Guid UserId { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (HasNoTrn != true)
        {
            return RedirectToPage("/Admin/User", new { UserId });
        }

        var user = await _dbContext.Users.SingleAsync(u => u.UserId == UserId);

        user.TrnLookupStatus = TrnLookupStatus.Failed;
        user.TrnAssociationSource = TrnAssociationSource.SupportUi;
        user.Updated = _clock.UtcNow;

        _dbContext.AddEvent(new Events.UserUpdatedEvent()
        {
            Source = Events.UserUpdatedEventSource.SupportUi,
            CreatedUtc = _clock.UtcNow,
            Changes = Events.UserUpdatedEventChanges.TrnLookupStatus,
            User = Events.User.FromModel(user),
            UpdatedByClientId = null,
            UpdatedByUserId = User.GetUserId()
        });

        await _dbContext.SaveChangesAsync();

        TempData.SetFlashSuccess("User marked as non DQT");

        return RedirectToPage("/Admin/User", new { UserId });
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == UserId);

        if (user is null || user.UserType != UserType.Default)
        {
            context.Result = NotFound();
            return;
        }

        if (user.Trn is not null)
        {
            context.Result = BadRequest();
            return;
        }

        await next();
    }
}
