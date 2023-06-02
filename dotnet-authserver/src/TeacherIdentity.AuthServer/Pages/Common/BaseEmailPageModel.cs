using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Common;

public class BaseEmailPageModel : BaseEmailPinGenerationPageModel
{
    public BaseEmailPageModel(
        IUserVerificationService userVerificationService,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, dbContext)
    {
    }

    public async Task<PinGenerationResultAction> GenerateEmailPinForNewEmail(
        string email,
        string fieldName = "Email",
        bool allowInstitutionEmails = false)
    {
        var emailPinGenerationFailedReasons = await GenerateEmailPin(email, allowInstitutionEmails);

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

            case EmailPinGenerationFailedReason.NonPersonalAddress:
                ModelState.AddModelError(fieldName, "Enter a personal email address not one from a work or education setting.");
                return PinGenerationResultAction.Failed(this.PageWithErrors());

            case EmailPinGenerationFailedReason.InvalidAddress:
                ModelState.AddModelError(fieldName, "Enter a valid email address");
                return PinGenerationResultAction.Failed(this.PageWithErrors());

            default:
                throw new NotImplementedException($"Unknown {nameof(EmailPinGenerationFailedReason)}: '{emailPinGenerationFailedReasons}'.");
        }
    }

    public async Task<User?> FindUserByEmailAddress(string email)
    {
        return await DbContext.Users.SingleOrDefaultAsync(u => u.EmailAddress == email);
        // return userWithNewEmail is not null && userWithNewEmail.UserId != User.GetUserId();
    }
}
