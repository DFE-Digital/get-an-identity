using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[BindProperties]
[RequiresTrnLookup]
public class HasNiNumberPage : PageModel
{
    private IdentityLinkGenerator _linkGenerator;
    public HasNiNumberPage(IdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    [Display(Name = "Do you have a National Insurance number?")]
    [Required(ErrorMessage = "Tell us if you have a National Insurance number")]
    public bool? HasNiNumber { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().OnHasNationalInsuranceNumberSet((bool)HasNiNumber!);

        return (bool)HasNiNumber!
            ? Redirect(_linkGenerator.RegisterNiNumber())
            : Redirect(_linkGenerator.RegisterHasTrn());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.DateOfBirthSet)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterDateOfBirth());
        }
    }
}
