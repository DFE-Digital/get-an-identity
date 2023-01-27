using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin.MergeUser;

[BindProperties]
public class Merge : PageModel
{
    public Guid[]? UserIds;

    private readonly TeacherIdentityServerDbContext _dbContext;

    public Merge(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
    }

    [Display(Name = "Select which user to merge")]
    [Required(ErrorMessage = "Tell us which user you want to merge")]
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

        HttpContext.Session.Remove(Confirm.MergeTrnKey);

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
            .Where(u => u.UserId != UserId && u.IsDeleted == false && u.UserType == UserType.Default)
            .Select(u => u.UserId).ToArray();

        await next();
    }
}
