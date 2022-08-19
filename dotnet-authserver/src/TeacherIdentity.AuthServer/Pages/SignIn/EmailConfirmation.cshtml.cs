using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class EmailConfirmationModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IEmailVerificationService _emailConfirmationService;

    public EmailConfirmationModel(
        TeacherIdentityServerDbContext dbContext,
        IEmailVerificationService emailConfirmationService,
        IDqtApiClient dqtApiClient)
    {
        _dbContext = dbContext;
        _emailConfirmationService = emailConfirmationService;
        _dqtApiClient = dqtApiClient;
    }

    public string? Email => HttpContext.GetAuthenticationState().EmailAddress;

    [BindProperty]
    [Display(Name = "Enter your code")]
    public string? Code { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        ValidateCode();

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var verifyPinFailedReasons = await _emailConfirmationService.VerifyPin(Email!, Code!);

        if (verifyPinFailedReasons != PinVerificationFailedReasons.None)
        {
            var generateAnotherCode = verifyPinFailedReasons.HasFlag(PinVerificationFailedReasons.ExpiredLessThanTwoHoursAgo);

            if (generateAnotherCode)
            {
                await _emailConfirmationService.GeneratePin(Email!);
                ModelState.AddModelError(nameof(Code), "The security code has expired. New code sent.");
            }
            else
            {
                ModelState.AddModelError(nameof(Code), "Enter a correct security code");
            }

            return this.PageWithErrors();
        }

        var authenticationState = HttpContext.GetAuthenticationState();

        var user = await _dbContext.Users.Where(u => u.EmailAddress == Email).SingleOrDefaultAsync();
        if (user is not null)
        {
            // N.B. If the user didn't match to a TRN when they registered then we won't get a result back from this API call
            var dqtIdentityInfo = await _dqtApiClient.GetTeacherIdentityInfo(user.UserId);

            await HttpContext.SignInUser(user, firstTimeUser: false, dqtIdentityInfo?.Trn);
        }
        else
        {
            authenticationState.EmailAddressVerified = true;
            authenticationState.FirstTimeUser = true;
        }

        return Redirect(authenticationState.GetNextHopUrl(Url));
    }

    private void ValidateCode()
    {
        if (string.IsNullOrEmpty(Code))
        {
            ModelState.AddModelError(nameof(Code), "Enter a correct security code");
        }
        else if (!Code.All(c => c >= '0' && c <= '9'))
        {
            ModelState.AddModelError(nameof(Code), "The code must be 5 numbers");
        }
        else if (Code.Length < 5)
        {
            ModelState.AddModelError(nameof(Code), "You’ve not entered enough numbers, the code must be 5 numbers");
        }
        else if (Code.Length > 5)
        {
            ModelState.AddModelError(nameof(Code), "You’ve entered too many numbers, the code must be 5 numbers");
        }
    }
}
