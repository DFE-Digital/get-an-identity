using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Services.Zendesk;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.EmailVerified)]
public class NoMatch : TrnCreateUserPageModel
{
    public NoMatch(
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IUserVerificationService userVerificationService,
        IZendeskApiWrapper zendeskApiWrapper)
        : base(linkGenerator, dbContext, clock, userVerificationService, zendeskApiWrapper)
    {
    }

    public string? EmailAddress => HttpContext.GetAuthenticationState().EmailAddress;
    public string? OfficialName => HttpContext.GetAuthenticationState().GetOfficialName();
    public string? PreviousOfficialName => HttpContext.GetAuthenticationState().GetPreviousOfficialName();
    public string? PreferredName => HttpContext.GetAuthenticationState().GetPreferredName();
    public DateOnly? DateOfBirth => HttpContext.GetAuthenticationState().DateOfBirth;
    public bool? HaveNationalInsuranceNumber => HttpContext.GetAuthenticationState().HasNationalInsuranceNumber;
    public string? NationalInsuranceNumber => HttpContext.GetAuthenticationState().NationalInsuranceNumber;
    public bool? AwardedQts => HttpContext.GetAuthenticationState().AwardedQts;
    public string? IttProviderName => HttpContext.GetAuthenticationState().IttProviderName;

    [BindProperty]
    [Display(Name = "Do you want to change something and try again?")]
    [Required(ErrorMessage = "Do you want to change something and try again?")]
    public bool? HasChangesToMake { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        if (HasChangesToMake == true)
        {
            return Redirect(LinkGenerator.TrnCheckAnswers());
        }

        return await TryCreateUser();
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        // We require all questions to have been answered and to have failed to find a TRN
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
            not { TrnLookupStatus: TrnLookupStatus.Pending or TrnLookupStatus.None } => Redirect(LinkGenerator.TrnCheckAnswers()),
            _ => null
        };
    }
}
