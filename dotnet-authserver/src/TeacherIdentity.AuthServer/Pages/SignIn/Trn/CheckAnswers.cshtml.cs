using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
public class CheckAnswers : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;

    public CheckAnswers(IIdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public string? BackLink => (HttpContext.GetAuthenticationState().HaveIttProvider == true)
        ? _linkGenerator.TrnIttProvider()
        : _linkGenerator.TrnAwardedQts();
    public string? EmailAddress => HttpContext.GetAuthenticationState().EmailAddress;
    public string? OfficialName => HttpContext.GetAuthenticationState().GetOfficialName();
    public string? PreviousOfficialName => HttpContext.GetAuthenticationState().GetPreviousOfficialName();
    public string? PreferredName => HttpContext.GetAuthenticationState().GetPreferredName();
    public DateOnly? DateOfBirth => HttpContext.GetAuthenticationState().DateOfBirth;
    public string? NationalInsuranceNumber => HttpContext.GetAuthenticationState().NationalInsuranceNumber;
    public bool? AwardedQts => HttpContext.GetAuthenticationState().AwardedQts;
    public string? IttProviderName => HttpContext.GetAuthenticationState().IttProviderName;


    public void OnGet()
    {
    }

    public void OnPost()
    {
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
