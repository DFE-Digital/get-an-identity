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

    public ICollection<Guid>? UserIds { get; set; }

    [BindProperty]
    [Display(Name = "Select which user to merge")]
    [Required(ErrorMessage = "Select which user you want to merge")]
    public Guid? UserIdToMerge { get; set; }

    [FromRoute]
    public Guid UserId { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        if (UserIds == null || !UserIds.Contains((Guid)UserIdToMerge!))
        {
            ModelState.AddModelError(nameof(UserIdToMerge), "You must select a user ID from the given list");
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

        UserIds = _dbContext.Users
            .Where(u => u.UserId != UserId && u.UserType == UserType.Default)
            .Select(u => u.UserId)
            .ToArray();

        await next();
    }
}
