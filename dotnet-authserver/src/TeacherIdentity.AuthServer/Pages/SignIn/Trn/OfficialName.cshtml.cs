using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]

public class OfficialName : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;

    public OfficialName(IIdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter your first name")]
    public string? OfficialFirstName { get; set; }

    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter your last name")]
    public string? OfficialLastName { get; set; }

    [Display(Name = "Previous first name (optional)")]
    public string? PreviousOfficialFirstName { get; set; }

    [Display(Name = "Previous last name (optional)")]
    public string? PreviousOfficialLastName { get; set; }

    public bool HasPreviousName { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().OnOfficialNameSet(
            OfficialFirstName!,
            OfficialLastName!,
            HasPreviousName ? PreviousOfficialFirstName : null,
            HasPreviousName ? PreviousOfficialLastName : null);

        return Redirect(_linkGenerator.TrnPreferredName());
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
}
