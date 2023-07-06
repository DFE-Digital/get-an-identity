using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Services.TrnTokens;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
[BindProperties]
public class GenerateTrnTokenModel : PageModel
{
    private readonly TrnTokenService _trnTokenService;

    public GenerateTrnTokenModel(TrnTokenService trnTokenService)
    {
        _trnTokenService = trnTokenService;
    }

    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter the email address")]
    public string? Email { get; set; }

    [Display(Name = "TRN")]
    [Required(ErrorMessage = "Enter the TRN")]
    public string? Trn { get; set; }

    [Display(Name = "TRN token")]
    public string? TrnToken { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        TrnToken = (await _trnTokenService.GenerateToken(Email!, Trn!, apiClientId: null, currentUserId: User.GetUserId())).TrnToken;

        return Page();
    }
}
