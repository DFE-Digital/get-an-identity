using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Common;

public class BasePhonePageModel : PageModel
{
    private IUserVerificationService _userVerificationService;
    private TeacherIdentityServerDbContext _dbContext;

    public BasePhonePageModel(
        IUserVerificationService userVerificationService,
        TeacherIdentityServerDbContext dbContext)
    {
        _userVerificationService = userVerificationService;
        _dbContext = dbContext;
    }

    [BindProperty]
    [Display(Name = "Mobile number", Description = "For international numbers include the country code")]
    [Required(ErrorMessage = "Enter your mobile phone number")]
    [MobilePhone(ErrorMessage = "Enter a valid mobile phone number")]
    public string? MobileNumber { get; set; }

    public async Task<PinGenerationResultAction> GenerateSmsPinForNewPhone(MobileNumber mobileNumber, string fieldName = "MobileNumber")
    {
        var pinGenerationResult = await _userVerificationService.GenerateSmsPin(mobileNumber);

        switch (pinGenerationResult.FailedReason)
        {
            case PinGenerationFailedReason.None:
                return PinGenerationResultAction.Succeeded();

            case PinGenerationFailedReason.RateLimitExceeded:
                return PinGenerationResultAction.Failed(new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyRequests"
                });

            case PinGenerationFailedReason.InvalidAddress:
                ModelState.AddModelError(fieldName, "Enter a valid mobile phone number");
                return PinGenerationResultAction.Failed(this.PageWithErrors());

            default:
                throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReason)}: '{pinGenerationResult.FailedReason}'.");
        }
    }

    public async Task<User?> FindUserByMobileNumber(MobileNumber mobileNumber)
    {
        return await _dbContext.Users.SingleOrDefaultAsync(u => u.NormalizedMobileNumber! == mobileNumber);
    }
}
