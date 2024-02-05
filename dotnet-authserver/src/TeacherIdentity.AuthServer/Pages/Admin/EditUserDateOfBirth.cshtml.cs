using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class EditUserDateOfBirthModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public EditUserDateOfBirthModel(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [FromRoute]
    public Guid UserId { get; set; }

    [BindProperty]
    [Display(Name = "Change date of birth")]
    [Required(ErrorMessage = "Enter a date of birth")]
    public DateOnly? DateOfBirth { get; set; }

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

        var changes = user.DateOfBirth != DateOfBirth ? UserUpdatedEventChanges.DateOfBirth : UserUpdatedEventChanges.None;

        user.DateOfBirth = DateOfBirth!.Value;

        if (changes != UserUpdatedEventChanges.None)
        {
            _dbContext.AddEvent(new UserUpdatedEvent()
            {
                Source = UserUpdatedEventSource.SupportUi,
                UpdatedByClientId = null,
                UpdatedByUserId = User.GetUserId(),
                CreatedUtc = _clock.UtcNow,
                User = Events.User.FromModel(user),
                Changes = changes
            });

            await _dbContext.SaveChangesAsync();

            TempData.SetFlashSuccess("Date of birth changed successfully");
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
