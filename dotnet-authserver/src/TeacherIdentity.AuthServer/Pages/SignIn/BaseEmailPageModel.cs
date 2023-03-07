using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class BaseEmailPageModel : BaseEmailPinGenerationPageModel
{
    public BaseEmailPageModel(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, linkGenerator, dbContext)
    {
    }

    public async Task<EmailPinGenerationResult> GenerateEmailPinForNewEmail(string email)
    {
        var emailPinGenerationFailedReasons = await GenerateEmailPin(email, true);

        switch (emailPinGenerationFailedReasons)
        {
            case EmailPinGenerationFailedReason.None:
                return EmailPinGenerationResult.Succeeded();

            case EmailPinGenerationFailedReason.RateLimitExceeded:
                return EmailPinGenerationResult.Failed(new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyRequests"
                });

            case EmailPinGenerationFailedReason.NonPersonalAddress:
                ModelState.AddModelError(nameof(email), "Enter a personal email address not one from a work or education setting.");
                return EmailPinGenerationResult.Failed(this.PageWithErrors());

            case EmailPinGenerationFailedReason.InvalidAddress:
                ModelState.AddModelError(nameof(email), "Enter a valid email address");
                return EmailPinGenerationResult.Failed(this.PageWithErrors());

            default:
                throw new NotImplementedException($"Unknown {nameof(EmailPinGenerationFailedReason)}: '{emailPinGenerationFailedReasons}'.");
        }
    }
}
