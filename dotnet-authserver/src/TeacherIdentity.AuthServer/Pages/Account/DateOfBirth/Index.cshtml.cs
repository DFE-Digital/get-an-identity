using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Account.DateOfBirth;

[BindProperties]
public class DateOfBirthPage : PageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly ProtectedStringFactory _protectedStringFactory;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;

    public DateOfBirthPage(
        IIdentityLinkGenerator linkGenerator,
        ProtectedStringFactory protectedStringFactory,
        TeacherIdentityServerDbContext dbContext,
        IDqtApiClient dqtApiClient)
    {
        _linkGenerator = linkGenerator;
        _protectedStringFactory = protectedStringFactory;
        _dbContext = dbContext;
        _dqtApiClient = dqtApiClient;
    }

    [Display(Name = "Your date of birth", Description = "For example, 27 3 1987")]
    [Required(ErrorMessage = "Enter your date of birth")]
    [IsPastDate(typeof(DateOnly), ErrorMessage = "Your date of birth must be in the past")]
    public DateOnly? DateOfBirth { get; set; }

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }
    public string? SafeReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var protectedDateOfBirth = _protectedStringFactory.CreateFromPlainValue(DateOfBirth.ToString()!);

        return Redirect(_linkGenerator.AccountDateOfBirthConfirm(protectedDateOfBirth, ReturnUrl));
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var b = await ChangeDateOfBirthEnabled();
        if (!await ChangeDateOfBirthEnabled())
        {
            context.Result = BadRequest();
            return;
        }

        SafeReturnUrl = !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/account";
        await next();
    }

    private async Task<bool> ChangeDateOfBirthEnabled()
    {
        var userId = User.GetUserId()!.Value;

        var user = await _dbContext.Users
            .Where(u => u.UserId == userId)
            .Select(u => new
            {
                u.DateOfBirth,
                u.Trn
            })
            .SingleAsync();

        var trn = user.Trn;

        if (trn is null)
        {
            return true;
        }

        var dqtUser = await _dqtApiClient.GetTeacherByTrn(trn) ??
                      throw new Exception($"User with TRN '{trn}' cannot be found in DQT.");

        return !user.DateOfBirth.Equals(dqtUser.DateOfBirth) && !dqtUser.PendingDateOfBirthChange;
    }
}
