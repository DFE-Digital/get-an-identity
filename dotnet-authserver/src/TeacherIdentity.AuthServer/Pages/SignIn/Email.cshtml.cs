using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[BindProperties]
[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.None)]
public class EmailModel : PageModel
{
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public EmailModel(
        IEmailVerificationService emailVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext)
    {
        _emailVerificationService = emailVerificationService;
        _linkGenerator = linkGenerator;
        _dbContext = dbContext;
    }

    [Display(Name = "Enter your email address", Description = "Use your personal email address. This is so you can keep these sign in details should you change jobs.")]
    [Required(ErrorMessage = "Enter your email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        if (_invalidEmailPrefixes.Contains(Email!.Split("@")[0]))
        {
            var existingUser = await _dbContext.Users.Where(user => user.EmailAddress == Email).SingleOrDefaultAsync();
            if (existingUser is null)
            {
                ModelState.AddModelError(nameof(Email), "Enter a personal email address. It cannot be one that other people may get access to.");
                return this.PageWithErrors();
            }
        }

        HttpContext.GetAuthenticationState().OnEmailSet(Email!);

        var pinGenerationResult = await _emailVerificationService.GeneratePin(Email!);

        if (pinGenerationResult.FailedReasons != PinGenerationFailedReasons.None)
        {
            if (pinGenerationResult.FailedReasons == PinGenerationFailedReasons.RateLimitExceeded)
            {
                return new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyRequests"
                };
            }

            throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReasons)}: '{pinGenerationResult.FailedReasons}'.");
        }

        return Redirect(_linkGenerator.EmailConfirmation());
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
