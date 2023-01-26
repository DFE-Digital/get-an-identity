using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

public class CheckAnswers : TrnCreateUserPageModel
{
    public CheckAnswers(
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IEmailVerificationService emailVerificationService)
        : base(linkGenerator, dbContext, clock, emailVerificationService)
    {
    }

    public string BackLink => (HttpContext.GetAuthenticationState().HaveIttProvider == true)
        ? LinkGenerator.TrnIttProvider()
        : LinkGenerator.TrnAwardedQts();

    public string? EmailAddress => HttpContext.GetAuthenticationState().EmailAddress;
    public string? OfficialName => HttpContext.GetAuthenticationState().GetOfficialName();
    public string? PreviousOfficialName => HttpContext.GetAuthenticationState().GetPreviousOfficialName();
    public string? PreferredName => HttpContext.GetAuthenticationState().GetPreferredName();
    public DateOnly? DateOfBirth => HttpContext.GetAuthenticationState().DateOfBirth;
    public bool? HaveNationalInsuranceNumber => HttpContext.GetAuthenticationState().HaveNationalInsuranceNumber;
    public string? NationalInsuranceNumber => HttpContext.GetAuthenticationState().NationalInsuranceNumber;
    public bool? AwardedQts => HttpContext.GetAuthenticationState().AwardedQts;
    public string? IttProviderName => HttpContext.GetAuthenticationState().IttProviderName;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrEmpty(HttpContext.GetAuthenticationState().Trn))
        {
            return Redirect(LinkGenerator.TrnNoMatch());
        }

        return await TryCreateUser();
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
}
