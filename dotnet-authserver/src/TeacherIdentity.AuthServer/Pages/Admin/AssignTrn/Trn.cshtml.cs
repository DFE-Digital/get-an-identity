using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Admin.AssignTrn;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class TrnModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;

    public TrnModel(TeacherIdentityServerDbContext dbContext, IDqtApiClient dqtApiClient)
    {
        _dbContext = dbContext;
        _dqtApiClient = dqtApiClient;
    }

    [BindProperty]
    [Display(Name = "What is their teacher reference number (TRN)?")]
    [Required(ErrorMessage = "Enter a TRN")]
    [RegularExpression(@"^\d{7}$", ErrorMessage = "TRN must be 7 digits")]
    public string? Trn { get; set; }

    [FromRoute]
    public Guid UserId { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var dqtTeacher = await _dqtApiClient.GetTeacherByTrn(Trn!);

        if (dqtTeacher is null)
        {
            ModelState.AddModelError(nameof(Trn), "TRN does not exist");
            return this.PageWithErrors();
        }

        var otherOtherWithTrn = await _dbContext.Users.SingleOrDefaultAsync(u => u.Trn == Trn);

        if (otherOtherWithTrn is not null)
        {
            ModelState.AddModelError(nameof(Trn), "TRN is assigned to another user");
            return this.PageWithErrors();
        }

        return RedirectToPage("Confirm", new { UserId, Trn });
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == UserId);

        if (user is null || user.UserType != UserType.Default)
        {
            context.Result = NotFound();
            return;
        }

        if (user.Trn is not null)
        {
            context.Result = BadRequest();
            return;
        }

        await next();
    }
}
