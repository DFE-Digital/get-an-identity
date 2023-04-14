using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[BindProperties]
[RequiresTrnLookup]
public class HasQtsPage : PageModel
{
    private IdentityLinkGenerator _linkGenerator;

    public HasQtsPage(IdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    [BindNever]
    public string BackLink => HttpContext.GetAuthenticationState().HasTrn == true
        ? _linkGenerator.RegisterTrn()
        : _linkGenerator.RegisterHasTrn();

    [Display(Name = "Have you been awarded qualified teacher status (QTS)?")]
    [Required(ErrorMessage = "Tell us if you have been awarded qualified teacher status (QTS)")]
    public bool? AwardedQts { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().OnAwardedQtsSet((bool)AwardedQts!);

        return (bool)AwardedQts!
            ? Redirect(_linkGenerator.RegisterIttProvider())
            : Redirect(_linkGenerator.RegisterCheckAnswers());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.HasTrnSet)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterHasTrn());
            return;
        }

        if (authenticationState.StatedTrn is null)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterTrn());
        }
    }
}
