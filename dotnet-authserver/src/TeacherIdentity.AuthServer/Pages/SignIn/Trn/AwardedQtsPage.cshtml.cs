using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.EmailVerified)]
public class AwardedQtsPage : TrnLookupPageModel
{
    public AwardedQtsPage(IIdentityLinkGenerator linkGenerator, TrnLookupHelper trnLookupHelper)
        : base(linkGenerator, trnLookupHelper)
    {
    }

    [BindNever]
    public string BackLink => (HttpContext.GetAuthenticationState().HasNationalInsuranceNumber == true)
        ? LinkGenerator.TrnNiNumber()
        : LinkGenerator.TrnHasNiNumber();

    [Display(Name = "Have you been awarded qualified teacher status (QTS)?")]
    [Required(ErrorMessage = "Tell us if you have been awarded qualified teacher status (QTS)")]
    public bool? AwardedQts { get; set; }

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

        HttpContext.GetAuthenticationState().OnAwardedQtsSet((bool)AwardedQts!);

        return (bool)AwardedQts!
            ? Redirect(LinkGenerator.TrnIttProvider())
            : await TryFindTrn() ?? Redirect(LinkGenerator.TrnCheckAnswers());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.HasNationalInsuranceNumberSet)
        {
            context.Result = new RedirectResult(LinkGenerator.TrnHasNiNumber());
        }
        else if (authenticationState.HasNationalInsuranceNumber == true && !authenticationState.NationalInsuranceNumberSet)
        {
            context.Result = new RedirectResult(LinkGenerator.TrnNiNumber());
        }
    }

    private void SetDefaultInputValues()
    {
        AwardedQts ??= HttpContext.GetAuthenticationState().AwardedQts;
    }
}
