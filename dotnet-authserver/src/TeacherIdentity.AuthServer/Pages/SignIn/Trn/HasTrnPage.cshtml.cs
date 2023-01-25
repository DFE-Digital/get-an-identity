using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
public class HasTrnPage : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;

    public HasTrnPage(IIdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    // Properties are set in the order that they are declared. Because the value of HasTrn
    // is used in the conditional RequiredIfTrue attribute, it should be set first.
    [Display(Name = "Do you know your TRN?")]
    [Required(ErrorMessage = "Tell us if you know your TRN")]
    public bool? HasTrn { get; set; }

    [Display(Name = "What is your TRN?")]
    [RequiredIfTrue(nameof(HasTrn), ErrorMessage = "Enter your TRN")]
    [RegexIfTrue(nameof(HasTrn), @"\A\D*(\d{1}\D*){7}\D*\Z", ErrorMessage = "Your TRN number should contain 7 digits")]
    public string? StatedTrn { get; set; }

    public void OnGet()
    {
        SetDefaultInputValues();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().OnHasTrnSet((bool)HasTrn!);

        if (HasTrn == true)
        {
            HttpContext.GetAuthenticationState().StatedTrn = StatedTrn;
        }

        return Redirect(_linkGenerator.TrnOfficialName());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        // We expect to have a verified email at this point but we shouldn't have completed the TRN lookup
        if (string.IsNullOrEmpty(authenticationState.EmailAddress) ||
            !authenticationState.EmailAddressVerified ||
            authenticationState.HaveCompletedTrnLookup)
        {
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(_linkGenerator));
        }
    }

    private void SetDefaultInputValues()
    {
        HasTrn ??= HttpContext.GetAuthenticationState().HasTrn;
        StatedTrn ??= HttpContext.GetAuthenticationState().StatedTrn;
    }
}
