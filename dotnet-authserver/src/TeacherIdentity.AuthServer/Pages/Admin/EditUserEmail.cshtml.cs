using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class EditUserEmailModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public EditUserEmailModel(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [FromRoute]
    public Guid UserId { get; set; }

    [BindProperty]
    [Display(Name = "Change email address")]
    [Required(ErrorMessage = "Enter an email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var user = await _dbContext.Users.Where(u => u.UserType == UserType.Default && u.UserId == UserId).SingleAsync();

        var changes = user.EmailAddress != Email ? UserUpdatedEventChanges.EmailAddress : UserUpdatedEventChanges.None;

        user.EmailAddress = Email!;

        if (changes != UserUpdatedEventChanges.None)
        {
            _dbContext.AddEvent(new UserUpdatedEvent()
            {
                Source = UserUpdatedEventSource.SupportUi,
                UpdatedByClientId = null,
                UpdatedByUserId = User.GetUserId()!.Value,
                CreatedUtc = _clock.UtcNow,
                User = Events.User.FromModel(user),
                Changes = changes
            });

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueIndexViolation("ix_users_email_address"))
            {
                ModelState.AddModelError(nameof(Email), "This email address is already in use - Enter a different email address");
                return this.PageWithErrors();
            }

            TempData.SetFlashSuccess("Email changed successfully");
        }

        return RedirectToPage("User", new { UserId });
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var user = await _dbContext.Users.Where(u => u.UserType == UserType.Default && u.UserId == UserId).SingleOrDefaultAsync();

        if (user is null)
        {
            context.Result = NotFound();
            return;
        }

        await next();
    }
}
