using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class BaseEmailPageModel : PageModel
{
    private readonly IUserVerificationService _userVerificationService;
    protected readonly IIdentityLinkGenerator LinkGenerator;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public BaseEmailPageModel(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext)
    {
        _userVerificationService = userVerificationService;
        LinkGenerator = linkGenerator;
        _dbContext = dbContext;
    }

    public async Task<EmailValidationResult> ValidateEmail(string email)
    {
        var emailParts = email!.Split("@");
        var emailPrefix = emailParts[0];
        var emailSuffix = emailParts[1];

        var invalidDomainCount = await _dbContext.EstablishmentDomains.Where(d => d.DomainName == emailSuffix).CountAsync();
        if (_invalidEmailPrefixes.Contains(emailPrefix) || invalidDomainCount > 0)
        {
            var existingUser = await _dbContext.Users.Where(user => user.EmailAddress == email).SingleOrDefaultAsync();
            if (existingUser is null)
            {
                ModelState.AddModelError(nameof(email), "Enter a personal email address not one from a work or education setting.");
                return EmailValidationResult.Failed(this.PageWithErrors());
            }
        }

        return await TryGenerateEmailPinForEmail(email);
    }

    private async Task<EmailValidationResult> TryGenerateEmailPinForEmail(string email)
    {
        var pinGenerationResult = await _userVerificationService.GenerateEmailPin(email);

        switch (pinGenerationResult.FailedReasons)
        {
            case PinGenerationFailedReasons.None:
                return EmailValidationResult.Success();

            case PinGenerationFailedReasons.RateLimitExceeded:
                return EmailValidationResult.Failed(new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyRequests"
                });

            case PinGenerationFailedReasons.InvalidAddress:
                ModelState.AddModelError(nameof(email), "Enter a valid email address");
                return EmailValidationResult.Failed(this.PageWithErrors());

            default:
                throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReasons)}: '{pinGenerationResult.FailedReasons}'.");
        }
    }

    private static readonly string[] _invalidEmailPrefixes =
    {
        "headteacher", "head.teacher", "head", "ht", "principal", "headofschool", "headmistress", "info", "office",
        "office1", "reception", "secretary", "admin", "admin1", "admin2", "administration", "adminoffice",
        "schooloffice", "schoolmanager", "enquiries", "enquiry", "generalenquiries", "post", "pa", "headspa",
        "headteacherpa", "contact", "school", "academy", "bursar", "finance", "hr", "secretary", "businessmanager",
        "deputy", "deputyhead", "exechead", "ceo", "cfo", "coo"
    };
}
