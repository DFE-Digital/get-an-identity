using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.EmailVerified)]
public class HasTrnPage : TrnLookupPageModel
{
    public HasTrnPage(IdentityLinkGenerator linkGenerator, TrnLookupHelper trnLookupHelper)
        : base(linkGenerator, trnLookupHelper)
    {
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

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().OnHasTrnSet(StatedTrn);

        return await TryFindTrn() ?? Redirect(LinkGenerator.TrnOfficialName());
    }

    private void SetDefaultInputValues()
    {
        HasTrn ??= HttpContext.GetAuthenticationState().HasTrn;
        StatedTrn ??= HttpContext.GetAuthenticationState().StatedTrn;
    }
}
