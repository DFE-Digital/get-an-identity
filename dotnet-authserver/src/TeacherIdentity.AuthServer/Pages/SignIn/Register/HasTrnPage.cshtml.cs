using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[RequiresTrnLookup]
public class HasTrnPage : PageModel
{
    private IdentityLinkGenerator _linkGenerator;
    public HasTrnPage(IdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    [BindProperty]
    [Display(Name = "Do you have a TRN?")]
    [Required(ErrorMessage = "Tell us if you know your TRN")]
    public bool? HasTrn { get; set; }

<<<<<<< HEAD
    public string BackLink => HttpContext.GetAuthenticationState().HasNationalInsuranceNumber == true
=======
    public string BackLink => HttpContext.GetAuthenticationState().HasNationalInsuranceNumberSet
>>>>>>> dad3c80 (Add has TRN page for register journey)
        ? _linkGenerator.RegisterNiNumber()
        : _linkGenerator.RegisterHasNiNumber();

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().OnHasTrnSet(HasTrn!.Value);

        return HasTrn.Value ?
            Redirect(_linkGenerator.RegisterTrn()) :
            Redirect(_linkGenerator.RegisterHaveQts());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.HasNationalInsuranceNumberSet)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterHasNiNumber());
            return;
        }

        if (authenticationState is { HasNationalInsuranceNumber: true, NationalInsuranceNumberSet: false })
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterNiNumber());
        }
    }
}
