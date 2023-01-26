using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
public class PreferredName : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;

    public PreferredName(IIdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public string? OfficialFirstName => HttpContext.GetAuthenticationState().OfficialFirstName;
    public string? OfficialLastName => HttpContext.GetAuthenticationState().OfficialLastName;

    // Properties are set in the order that they are declared. Because the value of HasPreferredName
    // is used in the conditional RequiredIfTrue attribute, it should be set first.
    [Display(Name = " ")]
    [Required(ErrorMessage = "Tell us if this is your preferred name")]
    public bool? HasPreferredName { get; set; }

    [Display(Name = "Preferred first name")]
    [RequiredIfTrue(nameof(HasPreferredName), ErrorMessage = "Enter your preferred first name")]
    public string? PreferredFirstName { get; set; }

    [Display(Name = "Preferred last name")]
    [RequiredIfTrue(nameof(HasPreferredName), ErrorMessage = "Enter your preferred last name")]
    public string? PreferredLastName { get; set; }

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

        if (HasPreferredName == true)
        {
            HttpContext.GetAuthenticationState().OnNameSet(PreferredFirstName!, PreferredLastName!);
        }

        return Redirect(_linkGenerator.TrnDateOfBirth());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        // We expect to have a verified email and official names at this point but we shouldn't have completed the TRN lookup
        if (string.IsNullOrEmpty(authenticationState.EmailAddress) ||
            !authenticationState.EmailAddressVerified ||
            string.IsNullOrEmpty(authenticationState.GetOfficialName()) ||
            authenticationState.HaveCompletedTrnLookup)
        {
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(_linkGenerator));
        }
    }

    private void SetDefaultInputValues()
    {
        PreferredFirstName ??= HttpContext.GetAuthenticationState().FirstName;
        PreferredLastName ??= HttpContext.GetAuthenticationState().LastName;

        HasPreferredName ??= !string.IsNullOrEmpty(PreferredFirstName) && !string.IsNullOrEmpty(PreferredLastName) ? true : null;
    }
}
