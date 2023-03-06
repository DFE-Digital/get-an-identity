using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class BaseExistingEmailPageModel : BaseEmailPinGenerationPageModel
{
    private readonly ILogger<BaseExistingEmailPageModel> _logger;

    public BaseExistingEmailPageModel(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        ILogger<BaseExistingEmailPageModel> logger) :
        base(userVerificationService, linkGenerator, dbContext)
    {
        _logger = logger;
    }

    public async Task<EmailPinGenerationResult> GenerateEmailPinForExistingEmail(string email)
    {
        var emailPinGenerationFailedReasons = await GenerateEmailPin(email, false);

        switch (emailPinGenerationFailedReasons)
        {
            case EmailPinGenerationFailedReasons.None:
                return EmailPinGenerationResult.Succeeded();

            case EmailPinGenerationFailedReasons.RateLimitExceeded:
                return EmailPinGenerationResult.Failed(new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyRequests"
                });

            case EmailPinGenerationFailedReasons.NonPersonalAddress:
            case EmailPinGenerationFailedReasons.InvalidAddress:
                _logger.LogWarning($"Validation failed for existing email: {email} ");
                return EmailPinGenerationResult.Failed(this.PageWithErrors());

            default:
                throw new NotImplementedException($"Unknown {nameof(EmailPinGenerationFailedReasons)}: '{emailPinGenerationFailedReasons}'.");
        }
    }
}
