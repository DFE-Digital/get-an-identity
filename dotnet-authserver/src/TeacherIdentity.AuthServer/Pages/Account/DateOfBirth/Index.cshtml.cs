using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.Account.DateOfBirth;

[BindProperties]
public class DateOfBirthPage : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;
    private readonly ProtectedStringFactory _protectedStringFactory;

    public DateOfBirthPage(IIdentityLinkGenerator linkGenerator, ProtectedStringFactory protectedStringFactory)
    {
        _linkGenerator = linkGenerator;
        _protectedStringFactory = protectedStringFactory;
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

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        SafeReturnUrl = !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/account";
    }
}
