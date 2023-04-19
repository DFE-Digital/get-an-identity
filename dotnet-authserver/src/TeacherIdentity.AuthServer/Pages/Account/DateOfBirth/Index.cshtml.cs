using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Account.DateOfBirth;

[BindProperties]
public class DateOfBirthPage : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;

    public DateOfBirthPage(
        IdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IDqtApiClient dqtApiClient)
    {
        _linkGenerator = linkGenerator;
        _dbContext = dbContext;
        _dqtApiClient = dqtApiClient;
    }

    [BindNever]
    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [Display(Name = "Your date of birth", Description = "For example, 27 3 1987")]
    [Required(ErrorMessage = "Enter your date of birth")]
    [IsPastDate(typeof(DateOnly), ErrorMessage = "Your date of birth must be in the past")]
    public DateOnly? DateOfBirth { get; set; }

    public void OnGet()
    {
        var userId = User.GetUserId(true);

        DateOfBirth = _dbContext.Users
        .Where(u => u.UserId == userId)
        .Select(u => u.DateOfBirth)
        .FirstOrDefault();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        return Redirect(_linkGenerator.AccountDateOfBirthConfirm(DateOfBirth!.Value, ClientRedirectInfo));
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (!await ChangeDateOfBirthEnabled())
        {
            context.Result = BadRequest();
            return;
        }

        await next();
    }

    private async Task<bool> ChangeDateOfBirthEnabled()
    {
        var trn = User.GetTrn(false);

        if (trn is null)
        {
            return true;
        }

        var dateOfBirth = await _dbContext.Users
            .Where(u => u.Trn == trn)
            .Select(u => u.DateOfBirth)
            .SingleAsync();

        var dqtUser = await _dqtApiClient.GetTeacherByTrn(trn) ??
                      throw new Exception($"User with TRN '{trn}' cannot be found in DQT.");

        return !dateOfBirth.Equals(dqtUser.DateOfBirth) && !dqtUser.PendingDateOfBirthChange;
    }
}
