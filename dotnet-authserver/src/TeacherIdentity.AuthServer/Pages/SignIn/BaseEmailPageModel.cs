using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class BaseEmailPageModel : PageModel
{
    private readonly IEmailVerificationService _emailVerificationService;
    protected readonly IIdentityLinkGenerator LinkGenerator;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public BaseEmailPageModel(
        IEmailVerificationService emailVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext)
    {
        _emailVerificationService = emailVerificationService;
        LinkGenerator = linkGenerator;
        _dbContext = dbContext;
    }

    public async Task<EmailValidationResult> ValidateEmail(string email)
    {
        if (_invalidEmailPrefixes.Contains(email!.Split("@")[0]))
        {
            var existingUser = await _dbContext.Users.Where(user => user.EmailAddress == email).SingleOrDefaultAsync();
            if (existingUser is null)
            {
                ModelState.AddModelError(nameof(email), "Enter a personal email address. It cannot be one that other people may get access to.");
                return EmailValidationResult.Failed(this.PageWithErrors());
            }
        }

        return await TryGeneratePinForEmail(email);
    }

    private async Task<EmailValidationResult> TryGeneratePinForEmail(string email)
    {
        var pinGenerationResult = await _emailVerificationService.GeneratePin(email);

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

            case PinGenerationFailedReasons.InvalidEmail:
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
