using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin.MergeUser;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
public class Merge : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public Merge(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [BindProperty]
    [Display(Name = "Enter the user ID to merge")]
    [Required(ErrorMessage = "Enter the user ID to merge")]
    public Guid? UserIdToMerge { get; set; }

    [FromRoute]
    public Guid UserId { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!UserIdToMerge.HasValue)
        {
            ModelState.AddModelError(nameof(UserIdToMerge), "Enter the user ID of the user to merge.");
        }
        else if (UserIdToMerge.Value == UserId)
        {
            ModelState.AddModelError(nameof(UserIdToMerge), "User cannot be merged with itself.");
        }

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        return RedirectToPage("Confirm", new { UserId, UserIdToMerge });
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == UserId);

        if (user is null || user.UserType != UserType.Default)
        {
            context.Result = NotFound();
            return;
        }

        await next();
    }
}
