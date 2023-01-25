using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
public class OfficialName : TrnLookupPageModel
{
    public OfficialName(
        IIdentityLinkGenerator linkGenerator,
        IDqtApiClient dqtApiClient,
        ILogger<TrnLookupPageModel> logger)
        : base(linkGenerator, dqtApiClient, logger)
    {
    }

    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter your first name")]
    public string? OfficialFirstName { get; set; }

    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter your last name")]
    public string? OfficialLastName { get; set; }

    [Display(Name = "Previous first name (optional)")]
    public string? PreviousOfficialFirstName { get; set; }

    [Display(Name = "Previous last name (optional)")]
    public string? PreviousOfficialLastName { get; set; }

    public HasPreviousNameOption? HasPreviousName { get; set; }

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

        HttpContext.GetAuthenticationState().OnHasPreviousNameSet((HasPreviousNameOption)HasPreviousName!);

        HttpContext.GetAuthenticationState().OnOfficialNameSet(
            OfficialFirstName!,
            OfficialLastName!,
            HasPreviousName == HasPreviousNameOption.Yes ? PreviousOfficialFirstName : null,
            HasPreviousName == HasPreviousNameOption.Yes ? PreviousOfficialLastName : null);

        return await TryFindTrn() ?? Redirect(LinkGenerator.TrnPreferredName());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        // We expect to have a verified email at this point but we shouldn't have completed the TRN lookup
        if (string.IsNullOrEmpty(authenticationState.EmailAddress) ||
            !authenticationState.EmailAddressVerified ||
            authenticationState.HaveCompletedTrnLookup)
        {
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(LinkGenerator));
        }
    }

    public enum HasPreviousNameOption
    {
        Yes,
        No,
        PreferNotToSay
    }

    private void SetDefaultInputValues()
    {
        OfficialFirstName ??= HttpContext.GetAuthenticationState().OfficialFirstName;
        OfficialLastName ??= HttpContext.GetAuthenticationState().OfficialLastName;
        PreviousOfficialFirstName ??= HttpContext.GetAuthenticationState().PreviousOfficialFirstName;
        PreviousOfficialLastName ??= HttpContext.GetAuthenticationState().PreviousOfficialLastName;
        HasPreviousName ??= HttpContext.GetAuthenticationState().HasPreviousName;
    }
}