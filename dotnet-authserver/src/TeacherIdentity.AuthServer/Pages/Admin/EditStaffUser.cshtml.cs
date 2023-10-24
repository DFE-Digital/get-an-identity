using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
[BindProperties]
public class EditStaffUserModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public EditStaffUserModel(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [FromRoute(Name = "userId")]
    public Guid UserId { get; set; }

    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter the user’s email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [MaxLength(Models.EmailAddress.EmailAddressMaxLength, ErrorMessage = "Email address must be 200 characters or less")]
    public string? Email { get; set; }

    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter the user’s first name")]
    [MaxLength(Models.User.FirstNameMaxLength, ErrorMessage = "First name must be 200 characters or less")]
    public string? FirstName { get; set; }

    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter the user’s last name")]
    [MaxLength(Models.User.LastNameMaxLength, ErrorMessage = "Last name must be 200 characters or less")]
    public string? LastName { get; set; }

    [Display(Name = "Roles")]
    public string[]? StaffRoles { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == UserId);

        if (user is null)
        {
            return NotFound();
        }

        Email = user.EmailAddress;
        FirstName = user.FirstName;
        LastName = user.LastName;
        StaffRoles = user.StaffRoles;

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var cleansedRoles = StaffRoles?.Where(role => Models.StaffRoles.All.Contains(role))?.ToArray() ?? Array.Empty<string>();

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == UserId);

        if (user is null)
        {
            return NotFound();
        }

        var changes = StaffUserUpdatedChanges.None |
            (user.EmailAddress != Email ? StaffUserUpdatedChanges.Email : 0) |
            (user.FirstName != FirstName ? StaffUserUpdatedChanges.FirstName : 0) |
            (user.LastName != LastName ? StaffUserUpdatedChanges.LastName : 0) |
            (!user.StaffRoles.SequenceEqualIgnoringOrder(cleansedRoles) ? StaffUserUpdatedChanges.StaffRoles : 0);

        user.EmailAddress = Email!;
        user.FirstName = FirstName!;
        user.LastName = LastName!;
        user.StaffRoles = cleansedRoles;

        if (changes != StaffUserUpdatedChanges.None)
        {
            _dbContext.AddEvent(new StaffUserUpdatedEvent()
            {
                UpdatedByUserId = User.GetUserId(),
                CreatedUtc = _clock.UtcNow,
                User = Events.User.FromModel(user),
                Changes = changes
            });

            using var suppressUniqueIndexViolationScope = SentryErrors.Suppress<DbUpdateException>(ex => ex.IsUniqueIndexViolation(Models.User.EmailAddressUniqueIndexName));
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex) when (suppressUniqueIndexViolationScope.IsExceptionSuppressed(ex))
            {
                ModelState.AddModelError(nameof(Email), "Another user has the specified email address");
                return this.PageWithErrors();
            }

            TempData.SetFlashSuccess("Staff user updated");
        }

        return RedirectToPage("Staff");
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (UserId == User.GetUserId())
        {
            context.Result = Forbid();
        }
    }
}
