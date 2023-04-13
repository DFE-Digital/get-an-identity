using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[BindProperties]
[RequiresTrnLookup]
public class TrnPage : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public TrnPage(IdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    [Display(Name = "Enter your TRN")]
    [Required(ErrorMessage = "Enter your TRN")]
    [RegularExpression(@"\A\D*(\d{1}\D*){7}\D*\Z", ErrorMessage = "Your TRN number should contain 7 digits")]
    public string? StatedTrn { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost(string submit)
    {
        if (submit == "trn_not_known")
        {
            HttpContext.GetAuthenticationState().OnHasTrnSet(false);
        }
        else
        {
            if (!ModelState.IsValid)
            {
                return this.PageWithErrors();
            }

            HttpContext.GetAuthenticationState().OnTrnSet(StatedTrn);
        }

        return Redirect(_linkGenerator.RegisterHasQts());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.HasTrnSet)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterHasTrn());
        }
    }
}
