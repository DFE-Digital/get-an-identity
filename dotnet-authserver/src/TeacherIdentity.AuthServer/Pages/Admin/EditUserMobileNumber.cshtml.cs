using System.ComponentModel.DataAnnotations;
using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class EditUserMobileNumber : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public EditUserMobileNumber(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [FromRoute]
    public Guid UserId { get; set; }

    [BindProperty]
    [Display(Name = "Change mobile number")]
    [MobilePhone(ErrorMessage = "Enter a valid phone number")]
    public string? MobileNumber { get; set; }

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

        if (!string.IsNullOrWhiteSpace(MobileNumber) && user.MobileNumber == MobileNumber)
        {
            ModelState.AddModelError(nameof(MobileNumber), "This phone number is already in use - Enter a different phone number.");
            return this.PageWithErrors();
        }

        var changes = user.MobileNumber != MobileNumber ? UserUpdatedEventChanges.MobileNumber : UserUpdatedEventChanges.None;

        if (changes == UserUpdatedEventChanges.None)
        {
            return RedirectToPage("User", new { UserId });
        }

        user.MobileNumber = MobileNumber;

        _dbContext.AddEvent(new UserUpdatedEvent
        {
            Source = UserUpdatedEventSource.SupportUi,
            UpdatedByClientId = null,
            UpdatedByUserId = User.GetUserId(),
            CreatedUtc = _clock.UtcNow,
            User = Events.User.FromModel(user),
            Changes = changes
        });

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (UniqueConstraintException ex) when (ex.IsUniqueIndexViolation(Models.User.MobileNumberUniqueIndexName))
        {
            ModelState.AddModelError(nameof(MobileNumber), "This phone number is already in use - Enter a different phone number.");
            return this.PageWithErrors();
        }

        TempData.SetFlashSuccess("Phone number changed successfully");

        return RedirectToPage("User", new { UserId });
    }
}
