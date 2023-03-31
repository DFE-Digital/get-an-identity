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
public class EditUserNameModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public EditUserNameModel(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [FromRoute]
    public Guid UserId { get; set; }

    public string? CurrentName { get; set; }

    [BindProperty]
    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter a first name")]
    [MaxLength(Models.User.FirstNameMaxLength, ErrorMessage = "First name must be 200 characters or less")]
    public string? NewFirstName { get; set; }

    [BindProperty]
    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter a last name")]
    [MaxLength(Models.User.LastNameMaxLength, ErrorMessage = "Last name must be 200 characters or less")]
    public string? NewLastName { get; set; }

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

        var changes = UserUpdatedEventChanges.None |
            (user.FirstName != NewFirstName ? UserUpdatedEventChanges.FirstName : UserUpdatedEventChanges.None) |
            (user.LastName != NewLastName ? UserUpdatedEventChanges.LastName : UserUpdatedEventChanges.None);

        user.FirstName = NewFirstName!;
        user.LastName = NewLastName!;

        if (changes != UserUpdatedEventChanges.None)
        {
            _dbContext.AddEvent(new UserUpdatedEvent()
            {
                Source = UserUpdatedEventSource.SupportUi,
                UpdatedByUserId = User.GetUserId()!.Value,
                UpdatedByClientId = null,
                CreatedUtc = _clock.UtcNow,
                User = Events.User.FromModel(user),
                Changes = changes
            });

            await _dbContext.SaveChangesAsync();

            TempData.SetFlashSuccess(new FlashSuccessData("Name changed successfully"));
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

        CurrentName = $"{user.FirstName} {user.LastName}";

        await next();
    }
}
