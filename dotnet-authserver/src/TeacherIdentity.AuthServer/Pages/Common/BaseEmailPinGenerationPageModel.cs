using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Common;

public class BaseEmailPinGenerationPageModel : PageModel
{
    private readonly IUserVerificationService _userVerificationService;
    protected readonly TeacherIdentityServerDbContext DbContext;

    public BaseEmailPinGenerationPageModel(
        IUserVerificationService userVerificationService,
        TeacherIdentityServerDbContext dbContext)
    {
        _userVerificationService = userVerificationService;
        DbContext = dbContext;
    }

    public async Task<EmailPinGenerationFailedReason> GenerateEmailPin(string email, bool requiresValidation = true)
    {
        if (requiresValidation && !await IsValidPersonalEmail(email))
        {
            return EmailPinGenerationFailedReason.NonPersonalAddress;
        }

        return await TryGenerateEmailPinForEmail(email);
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

    private async Task<EmailPinGenerationFailedReason> TryGenerateEmailPinForEmail(string email)
    {
        var pinGenerationResult = await _userVerificationService.GenerateEmailPin(email);
        return pinGenerationResult.FailedReason.ToEmailPinGenerationFailedReasons();
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
