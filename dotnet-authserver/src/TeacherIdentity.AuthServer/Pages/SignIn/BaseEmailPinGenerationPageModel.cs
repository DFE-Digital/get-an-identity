using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class BaseEmailPinGenerationPageModel : PageModel
{
    private readonly IUserVerificationService _userVerificationService;
    protected readonly IIdentityLinkGenerator LinkGenerator;
    protected readonly TeacherIdentityServerDbContext DbContext;

    public BaseEmailPinGenerationPageModel(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext)
    {
        _userVerificationService = userVerificationService;
        LinkGenerator = linkGenerator;
        DbContext = dbContext;
    }

    public async Task<EmailPinGenerationFailedReasons> GenerateEmailPin(string email, bool requiresValidation = true)
    {
        if (requiresValidation && !await IsValidPersonalEmail(email))
        {
            return EmailPinGenerationFailedReasons.NonPersonalAddress;
        }

        return await TryGenerateEmailPinForEmail(email, requiresValidation);
    }

    private async Task<bool> IsValidPersonalEmail(string email)
    {
        var emailParts = email.Split("@");
        var emailPrefix = emailParts[0];
        var emailSuffix = emailParts[1];

        var invalidDomainCount = await DbContext.EstablishmentDomains.Where(d => d.DomainName == emailSuffix).CountAsync();
        if (_invalidEmailPrefixes.Contains(emailPrefix) || invalidDomainCount > 0)
        {
            var existingUser = await DbContext.Users.Where(user => user.EmailAddress == email).SingleOrDefaultAsync();
            if (existingUser is null)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<EmailPinGenerationFailedReasons> TryGenerateEmailPinForEmail(string email, bool requiresValidation)
    {
        var pinGenerationResult = await _userVerificationService.GenerateEmailPin(email);
        return pinGenerationResult.FailedReasons.ToEmailPinGenerationFailedReasons();
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
