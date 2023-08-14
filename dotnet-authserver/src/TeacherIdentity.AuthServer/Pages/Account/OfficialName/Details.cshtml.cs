using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialName;

[VerifyQueryParameterSignature]
[CheckOfficialNameChangeIsEnabled]
public class Details : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public Details(IdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [BindProperty(SupportsGet = true)]
    [Display(Name = "First name", Description = "Or given names")]
    [Required(ErrorMessage = "Enter your first name")]
    [StringLength(100, ErrorMessage = "First name must be 100 characters or less")]
    public string? FirstName { get; set; }

    [BindProperty(SupportsGet = true)]
    [Display(Name = "Middle name")]
    [StringLength(100, ErrorMessage = "Middle name must be 100 characters or less")]
    public string? MiddleName { get; set; }

    [BindProperty(SupportsGet = true)]
    [Display(Name = "Last name", Description = "Or family names")]
    [Required(ErrorMessage = "Enter your last name")]
    [StringLength(100, ErrorMessage = "Last name must be 100 characters or less")]
    public string? LastName { get; set; }

    [FromQuery(Name = "fileName")]
    public string? FileName { get; set; }

    [FromQuery(Name = "fileId")]
    public string? FileId { get; set; }

    [FromQuery(Name = "preferredName")]
    public string? PreferredName { get; set; }

    [FromQuery(Name = "fromConfirmPage")]
    public bool FromConfirmPage { get; set; }

    private TeacherInfo? DqtUser { get; set; }

    public void OnGet()
    {
        // We may have been passed values for FirstName, LastName and maybe MiddleName e.g. when we came from the Confirm page.
        // If not, default to the current DQT values.
        if (FirstName is null && LastName is null)
        {
            FirstName = DqtUser!.FirstName;
            MiddleName = DqtUser.MiddleName;
            LastName = DqtUser.LastName;
        }
        ModelState.Clear();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        if (NamesUnchanged())
        {
            ModelState.AddModelError(nameof(FirstName), "The name entered matches your official name");
            return this.PageWithErrors();
        }

        return Redirect(FromConfirmPage && FileName is not null && FileId is not null ?
            _linkGenerator.AccountOfficialNameConfirm(FirstName!, MiddleName, LastName!, FileName, FileId, PreferredName!, ClientRedirectInfo) :
            _linkGenerator.AccountOfficialNameEvidence(FirstName!, MiddleName, LastName!, PreferredName, fromConfirmPage: false, ClientRedirectInfo));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (context.HttpContext.Items["DqtUser"] is TeacherInfo dqtUser)
        {
            DqtUser = dqtUser;
        }
        else
        {
            context.Result = new BadRequestResult();
        }
    }

    private bool NamesUnchanged()
    {
        return FirstName == DqtUser!.FirstName &&
               (MiddleName ?? string.Empty) == DqtUser.MiddleName &&
               LastName == DqtUser.LastName;
    }
}
