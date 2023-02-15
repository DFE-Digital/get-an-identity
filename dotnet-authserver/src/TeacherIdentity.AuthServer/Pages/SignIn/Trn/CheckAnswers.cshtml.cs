using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Services.Zendesk;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.EmailVerified)]
public class CheckAnswers : TrnCreateUserPageModel
{
    public CheckAnswers(
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IUserVerificationService userVerificationService,
        IZendeskApiWrapper zendeskApiWrapper)
        : base(linkGenerator, dbContext, clock, userVerificationService, zendeskApiWrapper)
    {
    }

    public string BackLink => (HttpContext.GetAuthenticationState().HasIttProvider == true)
        ? LinkGenerator.TrnIttProvider()
        : LinkGenerator.TrnAwardedQts();

    public string? EmailAddress => HttpContext.GetAuthenticationState().EmailAddress;
    public string? OfficialName => HttpContext.GetAuthenticationState().GetOfficialName();
    public string? PreviousOfficialName => HttpContext.GetAuthenticationState().GetPreviousOfficialName();
    public string? PreferredName => HttpContext.GetAuthenticationState().GetPreferredName();
    public DateOnly? DateOfBirth => HttpContext.GetAuthenticationState().DateOfBirth;
    public bool? HaveNationalInsuranceNumber => HttpContext.GetAuthenticationState().HasNationalInsuranceNumber;
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

        // We require all questions to have been answered OR to have found a TRN
        if (authenticationState.Trn is null)
        {
            context.Result = authenticationState switch
            {
                { HasTrnSet: false } => Redirect(LinkGenerator.TrnHasTrn()),
                { OfficialNameSet: false } => Redirect(LinkGenerator.TrnOfficialName()),
                { PreferredNameSet: false } => Redirect(LinkGenerator.TrnPreferredName()),
                { DateOfBirthSet: false } => Redirect(LinkGenerator.TrnDateOfBirth()),
                { HasNationalInsuranceNumberSet: false } => Redirect(LinkGenerator.TrnHasNiNumber()),
                { HasNationalInsuranceNumber: true } and { NationalInsuranceNumberSet: false } => Redirect(LinkGenerator.TrnNiNumber()),
                { AwardedQtsSet: false } => Redirect(LinkGenerator.TrnAwardedQts()),
                { AwardedQts: true } and { HasIttProviderSet: false } => Redirect(LinkGenerator.TrnIttProvider()),
                _ => null
            };
        }
    }
}
