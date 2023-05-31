using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Common;

public class BaseExistingEmailPageModel : BaseEmailPinGenerationPageModel
{
    public BaseExistingEmailPageModel(
        IUserVerificationService userVerificationService,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, dbContext)
    { }

    public async Task<PinGenerationResultAction> GenerateEmailPinForExistingEmail(string email)
    {
        var emailPinGenerationFailedReasons = await GenerateEmailPin(email, true);

        switch (emailPinGenerationFailedReasons)
        {
            case EmailPinGenerationFailedReason.None:
                return PinGenerationResultAction.Succeeded();

            case EmailPinGenerationFailedReason.RateLimitExceeded:
                return PinGenerationResultAction.Failed(new ViewResult()
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
