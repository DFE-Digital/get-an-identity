using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin.AssignTrn;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class RemoveModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private Models.User? _user;

    public RemoveModel(
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [FromRoute(Name = "userId")]
    public Guid UserId { get; set; }

    public string? Email => _user?.EmailAddress;

    public string? Name => $"{_user?.FirstName} {_user?.LastName}";

    public string? Trn => _user?.Trn;

    [BindProperty]
    public bool ConfirmRemoveTrn { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ConfirmRemoveTrn)
        {
            ModelState.AddModelError(nameof(ConfirmRemoveTrn), "Confirm you want to remove the TRN");
            return this.PageWithErrors();
        }

        _user!.Trn = null;
        _user.TrnAssociationSource = null;
        _user.TrnLookupStatus = null;
        _user.Updated = _clock.UtcNow;

        _dbContext.AddEvent(new UserUpdatedEvent()
        {
            Changes = UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus,
            CreatedUtc = _clock.UtcNow,
            Source = UserUpdatedEventSource.SupportUi,
            UpdatedByClientId = null,
            UpdatedByUserId = User.GetUserId(),
            User = _user
        });

        await _dbContext.SaveChangesAsync();

        TempData.SetFlashSuccess("TRN removed");
        return RedirectToPage("/Admin/User", new { UserId });
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        _user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == UserId);

        if (_user is null || _user.UserType != UserType.Default)
        {
            context.Result = NotFound();
            return;
        }

        if (_user.Trn is null)
        {
            context.Result = BadRequest();
            return;
        }

        await next();
    }
}
