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
    public string? OfficialName => GetFullName(HttpContext.GetAuthenticationState().OfficialFirstName, HttpContext.GetAuthenticationState().OfficialLastName);
    public string? PreviousOfficialName => GetFullName(HttpContext.GetAuthenticationState().PreviousOfficialFirstName, HttpContext.GetAuthenticationState().PreviousOfficialLastName);
    public string? PreferredName => GetFullName(HttpContext.GetAuthenticationState().FirstName, HttpContext.GetAuthenticationState().LastName);
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

    private string? GetFullName(string? firstName, string? lastName)
    {
        if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
        {
            return $"{firstName} {lastName}";
        }
        return firstName ?? (lastName ?? null);
    }
}
