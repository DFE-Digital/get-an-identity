using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin.MergeUser;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
public class ChooseTrn : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public ChooseTrn(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [BindProperty]
    [Display(Name = "Which TRN do you want to keep?")]
    [Required(ErrorMessage = "Select the TRN you want to keep")]
    public string? Trn { get; set; }

    [FromRoute]
    public Guid UserId { get; set; }

    public string? UserTrn { get; set; }

    [FromRoute]
    public Guid UserIdToMerge { get; set; }

    public string? UserToMergeTrn { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.Session.SetString(Confirm.ChosenTrnKey, Trn!);

        return RedirectToPage("Confirm", new { UserId, UserIdToMerge });
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var user = await GetUser(UserId);
        var userToMerge = await GetUser(UserIdToMerge);

        if (user == null || userToMerge == null)
        {
            context.Result = NotFound();
            return;
        }

        UserTrn = user.Trn;
        UserToMergeTrn = userToMerge.Trn;

        await next();
    }

    private async Task<User?> GetUser(Guid userId)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == userId);

        return (user is null || user.UserType != UserType.Default) ? null : user;
    }
}
