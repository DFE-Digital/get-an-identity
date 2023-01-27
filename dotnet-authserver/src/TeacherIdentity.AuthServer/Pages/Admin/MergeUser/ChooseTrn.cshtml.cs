using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin.MergeUser;

public class ChooseTrn : PageModel
{
    public new User? User { get; set; }
    public User? UserToMerge { get; set; }

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

    [FromRoute]
    public Guid UserIdToMerge { get; set; }


    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.Session.SetString(Confirm.MergeTrnKey, Trn!);

        return RedirectToPage("Confirm", new { UserId, UserIdToMerge });
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        User = await GetUser(UserId);
        UserToMerge = await GetUser(UserIdToMerge);

        if (User == null || UserToMerge == null)
        {
            context.Result = NotFound();
            return;
        }

        await next();
    }

    private async Task<User?> GetUser(Guid userId)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == userId);

        return (user is null || user.UserType != UserType.Default) ? null : user;
    }
}
