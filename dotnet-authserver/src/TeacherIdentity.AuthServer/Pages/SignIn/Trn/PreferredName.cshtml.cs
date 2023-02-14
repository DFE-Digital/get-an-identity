using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.EmailVerified)]
public class PreferredName : PageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;

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
    [StringLength(200, ErrorMessage = "Preferred first name must be 200 characters or less")]
    public string? PreferredFirstName { get; set; }

    [Display(Name = "Preferred last name")]
    [RequiredIfTrue(nameof(HasPreferredName), ErrorMessage = "Enter your preferred last name")]
    [StringLength(200, ErrorMessage = "Preferred last name must be 200 characters or less")]
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

        HttpContext.GetAuthenticationState().OnNameSet(
            HasPreferredName == true ? PreferredFirstName : null,
            HasPreferredName == true ? PreferredLastName : null);

        return Redirect(_linkGenerator.TrnDateOfBirth());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.OfficialNameSet)
        {
            context.Result = new RedirectResult(_linkGenerator.TrnOfficialName());
        }
    }

    private void SetDefaultInputValues()
    {
        PreferredFirstName ??= HttpContext.GetAuthenticationState().FirstName;
        PreferredLastName ??= HttpContext.GetAuthenticationState().LastName;

        HasPreferredName ??= !string.IsNullOrEmpty(PreferredFirstName) && !string.IsNullOrEmpty(PreferredLastName) ? true : null;
    }
}
