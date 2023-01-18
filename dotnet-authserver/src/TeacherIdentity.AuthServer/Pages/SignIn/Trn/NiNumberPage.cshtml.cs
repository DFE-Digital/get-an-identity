using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
public class NiNumberPage : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;

    public NiNumberPage(IIdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
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
            HttpContext.GetAuthenticationState().HaveNationalInsuranceNumber = false;
        }
        else
        {
            if (!ModelState.IsValid)
            {
                return this.PageWithErrors();
            }

            HttpContext.GetAuthenticationState().NationalInsuranceNumber = NiNumber!.ToUpper().Replace(" ", string.Empty);
        }

        return Redirect(_linkGenerator.TrnAwardedQts());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        // We expect to have a verified email and official names at this point but we shouldn't have completed the TRN lookup
        if (string.IsNullOrEmpty(authenticationState.EmailAddress) ||
            !authenticationState.EmailAddressVerified ||
            !authenticationState.HasOfficialName() ||
            authenticationState.HaveCompletedTrnLookup)
        {
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(_linkGenerator));
        }
    }
}
