using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
public class DateOfBirthPage : TrnLookupPageModel
{
    public DateOfBirthPage(
        IIdentityLinkGenerator linkGenerator,
        IDqtApiClient dqtApiClient,
        ILogger<TrnLookupPageModel> logger)
        : base(linkGenerator, dqtApiClient, logger)
    {
    }

    [Display(Name = "Your date of birth", Description = "For example, 27 3 1987")]
    [Required(ErrorMessage = "Enter your date of birth")]
    [IsPastDate(typeof(DateOnly), ErrorMessage = "Your date of birth must be in the past")]
    public DateOnly? DateOfBirth { get; set; }

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

        HttpContext.GetAuthenticationState().OnDateOfBirthSet((DateOnly)DateOfBirth!);

        return await TryFindTrn() ?? Redirect(LinkGenerator.TrnHaveNiNumber());
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
        DateOfBirth ??= HttpContext.GetAuthenticationState().DateOfBirth;
    }
}