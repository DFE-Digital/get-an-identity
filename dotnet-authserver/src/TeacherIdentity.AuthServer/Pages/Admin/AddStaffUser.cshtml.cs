using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
[BindProperties]
public class AddStaffUserModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public AddStaffUserModel(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter the user’s email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [MaxLength(200, ErrorMessage = "Email address must be 200 characters or less")]
    public string? Email { get; set; }

    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter the user’s first name")]
    [MaxLength(200, ErrorMessage = "First name must be 200 characters or less")]
    public string? FirstName { get; set; }

    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter the user’s last name")]
    [MaxLength(200, ErrorMessage = "Last name must be 200 characters or less")]
    public string? LastName { get; set; }

    [Display(Name = "Roles")]
    public string[]? StaffRoles { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var cleansedRoles = StaffRoles?.Where(role => Models.StaffRoles.All.Contains(role))?.ToArray() ?? Array.Empty<string>();
        var userId = Guid.NewGuid();

        var user = new Models.User()
        {
            Created = _clock.UtcNow,
            EmailAddress = Email!,
            FirstName = FirstName!,
            MiddleName = null,
            LastName = LastName!,
            StaffRoles = cleansedRoles,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Staff
        };

        _dbContext.Users.Add(user);

        _dbContext.AddEvent(new StaffUserAddedEvent()
        {
            AddedByUserId = User.GetUserId(),
            CreatedUtc = _clock.UtcNow,
            User = Events.User.FromModel(user)
        });

        using var suppressUniqueIndexViolationScope = SentryErrors.Suppress<DbUpdateException>(ex => ex.IsUniqueIndexViolation(Models.User.EmailAddressUniqueIndexName));
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (suppressUniqueIndexViolationScope.IsExceptionSuppressed(ex))
        {
            ModelState.AddModelError(nameof(Email), "A user already exists with the specified email address");
            return this.PageWithErrors();
        }

        TempData.SetFlashSuccess("Staff user added");
        return RedirectToPage("Staff");
    }
}
