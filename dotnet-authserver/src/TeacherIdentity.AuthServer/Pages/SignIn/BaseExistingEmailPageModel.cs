using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class BaseExistingEmailPageModel : BaseEmailPinGenerationPageModel
{
    public BaseExistingEmailPageModel(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, linkGenerator, dbContext)
    { }

    public async Task<EmailPinGenerationResult> GenerateEmailPinForExistingEmail(string email)
    {
        var emailPinGenerationFailedReasons = await GenerateEmailPin(email, false);

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

            case EmailPinGenerationFailedReason.InvalidAddress:
            case EmailPinGenerationFailedReason.NonPersonalAddress:
                throw new Exception($"Validation error thrown for existing email: {email}");

            default:
                throw new NotImplementedException($"Unknown {nameof(EmailPinGenerationFailedReason)}: '{emailPinGenerationFailedReasons}'.");
        }
    }
}
