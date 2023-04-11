using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Pages.SignIn.Trn;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[BindProperties]
[RequiresTrnLookup]
public class NiNumberPage : TrnLookupPageModel
{
    public NiNumberPage(IdentityLinkGenerator linkGenerator, TrnLookupHelper trnLookupHelper)
        : base(linkGenerator, trnLookupHelper)
    {
    }

    [Display(Name = "What is your National Insurance number?", Description = "It’s on your National Insurance card, benefit letter, payslip or P60. For example, ‘QQ 12 34 56 C’.")]
    [Required(ErrorMessage = "Enter a National Insurance number")]
    [RegularExpression(@"(?i)\A[a-z]{2}(?: [0-9]{2}){3} [a-d]{1}|[a-z]{2}[0-9]{6}[a-d]{1}\Z", ErrorMessage = "Enter a National Insurance number in the correct format")]
    public string? NiNumber { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost(string submit)
    {
        if (submit == "ni_number_not_known")
        {
            HttpContext.GetAuthenticationState().OnHasNationalInsuranceNumberSet(false);
        }
        else
        {
            if (!ModelState.IsValid)
            {
                return this.PageWithErrors();
            }

            HttpContext.GetAuthenticationState().OnNationalInsuranceNumberSet(NiNumber!);
        }

        return Redirect(LinkGenerator.RegisterHasTrn());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.HasNationalInsuranceNumberSet)
        {
            context.Result = new RedirectResult(LinkGenerator.RegisterHasNiNumber());
        }
    }
}
