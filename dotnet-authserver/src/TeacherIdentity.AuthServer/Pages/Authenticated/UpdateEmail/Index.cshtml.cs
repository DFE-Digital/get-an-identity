using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.Authenticated.UpdateEmail;

public class IndexModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ProtectedStringFactory _protectedStringFactory;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public IndexModel(
        TeacherIdentityServerDbContext dbContext,
        IEmailVerificationService emailVerificationService,
        ProtectedStringFactory protectedStringFactory,
        IIdentityLinkGenerator linkGenerator)
    {
        _dbContext = dbContext;
        _emailVerificationService = emailVerificationService;
        _protectedStringFactory = protectedStringFactory;
        _linkGenerator = linkGenerator;
    }

    [BindProperty]
    [Display(Name = "Enter your new email address")]
    [Required(ErrorMessage = "Enter your new email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    [FromQuery(Name = "cancelUrl")]
    public string? CancelUrl { get; set; }

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (ModelState[nameof(Email)]?.ValidationState == ModelValidationState.Valid)
        {
            // Check if email is already in use
            var userWithNewEmail = await _dbContext.Users.SingleOrDefaultAsync(u => u.EmailAddress == Email!);
            if (userWithNewEmail is not null && userWithNewEmail.UserId != User.GetUserId()!.Value)
            {
                ModelState.AddModelError(nameof(Email), "This email address is already in use - Enter a different email address");
            }
        }

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var result = await _emailVerificationService.GeneratePin(Email!);
        if (result.FailedReasons == PinGenerationFailedReasons.RateLimitExceeded)
        {
            return new ViewResult()
            {
                StatusCode = 429,
                ViewName = "TooManyRequests"
            };
        }

        var protectedEmail = _protectedStringFactory.CreateFromPlainValue(Email!);

        return Redirect(_linkGenerator.UpdateEmailConfirmation(protectedEmail, ReturnUrl, CancelUrl));
    }
}
