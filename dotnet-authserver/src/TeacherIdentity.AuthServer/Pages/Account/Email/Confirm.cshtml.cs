using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Account.Email;

[VerifyQueryParameterSignature]
public class Confirm : BasePinVerificationPageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly IClock _clock;

    public Confirm(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        TeacherIdentityServerDbContext dbContext,
        IdentityLinkGenerator linkGenerator,
        IClock clock) :
        base(userVerificationService, pinValidator)
    {
        _dbContext = dbContext;
        _linkGenerator = linkGenerator;
        _clock = clock;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [BindProperty]
    [Display(Name = "Confirmation code")]
    public override string? Code { get; set; }

    [FromQuery(Name = "email")]
    public string? Email { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        Code = Code?.Trim();
        ValidateCode();

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var verifyEmailPinFailedReasons = await UserVerificationService.VerifyEmailPin(Email!, Code!);

        if (verifyEmailPinFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(verifyEmailPinFailedReasons);
        }

        await UpdateUserEmail(User.GetUserId()!.Value);
        return Redirect(_linkGenerator.Account(ClientRedirectInfo));
    }

    private async Task UpdateUserEmail(Guid userId)
    {
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId);

        var newEmail = Email!;

        UserUpdatedEventChanges changes = UserUpdatedEventChanges.None;

        if (user.EmailAddress != newEmail)
        {
            changes |= UserUpdatedEventChanges.EmailAddress;
        }

        if (changes != UserUpdatedEventChanges.None)
        {
            user.EmailAddress = newEmail;
            user.Updated = _clock.UtcNow;

            _dbContext.AddEvent(new UserUpdatedEvent()
            {
                Source = UserUpdatedEventSource.ChangedByUser,
                CreatedUtc = _clock.UtcNow,
                Changes = changes,
                User = user,
                UpdatedByUserId = User.GetUserId()!.Value,
                UpdatedByClientId = null
            });

            await _dbContext.SaveChangesAsync();

            await HttpContext.SignInCookies(user, resetIssued: false);

            TempData.SetFlashSuccess("Your email address has been updated");
        }
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (Email is null)
        {
            context.Result = BadRequest();
        }
    }

    protected override Task<PinGenerationResult> GeneratePin()
    {
        return UserVerificationService.GenerateEmailPin(Email!);
    }
}
