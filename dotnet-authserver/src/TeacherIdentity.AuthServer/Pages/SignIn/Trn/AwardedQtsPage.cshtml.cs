using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
public class AwardedQtsPage : TrnLookupPageModel
{
    public AwardedQtsPage(IIdentityLinkGenerator linkGenerator, TrnLookupHelper trnLookupHelper)
        : base(linkGenerator, trnLookupHelper)
    {
    }

    [BindNever]
    public string BackLink => (HttpContext.GetAuthenticationState().HaveNationalInsuranceNumber == true)
        ? LinkGenerator.TrnNiNumber()
        : LinkGenerator.TrnHaveNiNumber();

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

        // We expect to have a verified email and official names at this point but we shouldn't have completed the TRN lookup
        if (string.IsNullOrEmpty(authenticationState.EmailAddress) ||
            !authenticationState.EmailAddressVerified ||
            string.IsNullOrEmpty(authenticationState.GetOfficialName()) ||
            authenticationState.HaveCompletedTrnLookup)
        {
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(LinkGenerator));
        }
    }

    private void SetDefaultInputValues()
    {
        AwardedQts ??= HttpContext.GetAuthenticationState().AwardedQts;
    }
}
