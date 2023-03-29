using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.EmailVerified)]
public class HasNiNumberPage : TrnLookupPageModel
{
    public HasNiNumberPage(IdentityLinkGenerator linkGenerator, TrnLookupHelper trnLookupHelper)
        : base(linkGenerator, trnLookupHelper)
    {
    }

    [Display(Name = "Do you have a National Insurance number?")]
    [Required(ErrorMessage = "Tell us if you have a National Insurance number")]
    public bool? HasNiNumber { get; set; }

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

        HttpContext.GetAuthenticationState().OnHasNationalInsuranceNumberSet((bool)HasNiNumber!);

        return (bool)HasNiNumber!
            ? Redirect(LinkGenerator.TrnNiNumber())
            : await TryFindTrn() ?? Redirect(LinkGenerator.TrnAwardedQts());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.DateOfBirthSet)
        {
            context.Result = new RedirectResult(LinkGenerator.TrnDateOfBirth());
        }
    }

    private void SetDefaultInputValues()
    {
        HasNiNumber ??= HttpContext.GetAuthenticationState().HasNationalInsuranceNumber;
    }
}
