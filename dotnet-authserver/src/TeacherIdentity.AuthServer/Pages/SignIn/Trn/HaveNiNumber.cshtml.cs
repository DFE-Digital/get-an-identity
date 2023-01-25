using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
public class HaveNiNumber : TrnLookupPageModel
{
    public HaveNiNumber(
        IIdentityLinkGenerator linkGenerator,
        IDqtApiClient dqtApiClient,
        ILogger<TrnLookupPageModel> logger)
        : base(linkGenerator, dqtApiClient, logger)
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

        HttpContext.GetAuthenticationState().OnHaveNationalInsuranceNumberSet((bool)HasNiNumber!);

        return (bool)HasNiNumber!
            ? Redirect(LinkGenerator.TrnNiNumber())
            : await TryFindTrn() ?? Redirect(LinkGenerator.TrnAwardedQts());
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
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(LinkGenerator));
        }
    }

    private void SetDefaultInputValues()
    {
        HasNiNumber ??= HttpContext.GetAuthenticationState().HaveNationalInsuranceNumber;
    }
}
