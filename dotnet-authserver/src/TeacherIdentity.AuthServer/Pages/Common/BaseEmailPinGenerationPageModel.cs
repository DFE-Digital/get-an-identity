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

    public async Task<EmailPinGenerationFailedReason> GenerateEmailPin(string email, bool allowInstitutionEmails = false)
    {
        if (!allowInstitutionEmails && await IsInstitutionEmail(email))
        {
            return EmailPinGenerationFailedReason.NonPersonalAddress;
        }

        return await TryGenerateEmailPinForEmail(email);
    }

    private async Task<EmailPinGenerationFailedReason> TryGenerateEmailPinForEmail(string email)
    {
        var pinGenerationResult = await _userVerificationService.GenerateEmailPin(email);
        return pinGenerationResult.FailedReason.ToEmailPinGenerationFailedReasons();
    }

    protected async Task<bool> IsInstitutionEmail(string email)
    {
        // if there's already a user with this email we do not consider it invalid
        if (await DbContext.Users.Where(user => user.EmailAddress == email).AnyAsync())
        {
            return false;
        }

        var emailParts = email.Split("@");
        var emailPrefix = emailParts[0];
        var emailSuffix = emailParts[1];

        var shouldBlockEstablishmentDomains = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>("BlockEstablishmentEmailDomains");

        if (shouldBlockEstablishmentDomains &&
            await DbContext.EstablishmentDomains.Where(d => d.DomainName == emailSuffix).AnyAsync())
        {
            return true;
        }

        return _invalidEmailPrefixes.Contains(emailPrefix);
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
