using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
public class AwardedQtsPage : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;

    public AwardedQtsPage(IIdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public string? BackLink => HttpContext.GetAuthenticationState().HaveNationalInsuranceNumber
        ? _linkGenerator.TrnNiNumber()
        : _linkGenerator.TrnHaveNiNumber();

    [Display(Name = "Have you been awarded qualified teacher status (QTS)?")]
    [Required(ErrorMessage = "Tell us if you have been awarded qualified teacher status (QTS)")]
    public bool? AwardedQts { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().AwardedQts = (bool)AwardedQts!;

        return (bool)AwardedQts!
            ? Redirect(_linkGenerator.TrnIttProvider())
            : Redirect(_linkGenerator.TrnCheckAnswers());
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
