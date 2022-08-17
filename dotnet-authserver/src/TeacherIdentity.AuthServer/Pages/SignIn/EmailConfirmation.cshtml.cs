using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class EmailConfirmationModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IEmailConfirmationService _emailConfirmationService;

    public EmailConfirmationModel(
        TeacherIdentityServerDbContext dbContext,
        IEmailConfirmationService emailConfirmationService,
        IDqtApiClient apiClient)
    {
        _dbContext = dbContext;
        _dqtApiClient = apiClient;
        _emailConfirmationService = emailConfirmationService;
    }

    public string? Email => HttpContext.GetAuthenticationState().EmailAddress;

    [BindProperty]
    [Display(Name = "Enter your code")]
    [Required(ErrorMessage = "Enter your code")]
    public string? Code { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        if (!await _emailConfirmationService.VerifyPin(Email!, Code!))
        {
            ModelState.AddModelError(nameof(Code), "TODO content: Code is incorrect or expired");
            return this.PageWithErrors();
        }

        var authenticationState = HttpContext.GetAuthenticationState();

        var user = await _dbContext.Users.Where(u => u.EmailAddress == Email).SingleOrDefaultAsync();
        if (user is not null)
        {
            // N.B. If the user didn't match to a TRN when they registered then we won't get a result back from this API call
            var dqtIdentityInfo = await _dqtApiClient.GetTeacherIdentityInfo(user.UserId);

            await HttpContext.SignInUser(user, dqtIdentityInfo?.Trn);

            authenticationState.FirstTimeUser = false;
        }
        else
        {
            authenticationState.EmailAddressConfirmed = true;
            authenticationState.FirstTimeUser = true;
        }

        return Redirect(authenticationState.GetNextHopUrl(Url));
    }
}
