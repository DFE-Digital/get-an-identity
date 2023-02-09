using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using static TeacherIdentity.AuthServer.AuthenticationState;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
[RequireAuthenticationMilestone(AuthenticationMilestone.EmailVerified)]
public class OfficialName : TrnLookupPageModel
{
    public OfficialName(IIdentityLinkGenerator linkGenerator, TrnLookupHelper trnLookupHelper)
        : base(linkGenerator, trnLookupHelper)
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

    [Required(ErrorMessage = "Tell us if you have changed your name")]
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

        HttpContext.GetAuthenticationState().OnOfficialNameSet(
            OfficialFirstName!,
            OfficialLastName!,
            (HasPreviousNameOption)HasPreviousName!,
            HasPreviousName == HasPreviousNameOption.Yes ? PreviousOfficialFirstName : null,
            HasPreviousName == HasPreviousNameOption.Yes ? PreviousOfficialLastName : null);

        return await TryFindTrn() ?? Redirect(LinkGenerator.TrnPreferredName());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.HasTrnSet)
        {
            context.Result = Redirect(LinkGenerator.TrnHasTrn());
        }
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
