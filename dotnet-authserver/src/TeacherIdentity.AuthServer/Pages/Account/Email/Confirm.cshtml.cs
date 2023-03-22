using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Account.Email;

public class Confirm : BasePinVerificationPageModel
{
    private TeacherIdentityServerDbContext _dbContext;
    private IClock _clock;

    public Confirm(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock) :
        base(userVerificationService, pinValidator)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [BindProperty]
    [Display(Name = "Confirmation code")]
    public override string? Code { get; set; }

    [FromQuery(Name = "email")]
    public ProtectedString? Email { get; set; }

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }
    public string? SafeReturnUrl { get; set; }


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

        var verifyEmailPinFailedReasons = await UserVerificationService.VerifyEmailPin(Email!.PlainValue, Code!);

        if (verifyEmailPinFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(verifyEmailPinFailedReasons);
        }

        await UpdateUserEmail(User.GetUserId()!.Value);
        return Redirect(SafeReturnUrl!);
    }

    private async Task UpdateUserEmail(Guid userId)
    {
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId);

        var newEmail = Email!.PlainValue;

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
            context.Result = new BadRequestResult();
            return;
        }

        SafeReturnUrl = !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/account";
    }

    protected override Task<PinGenerationResult> GeneratePin()
    {
        return UserVerificationService.GenerateEmailPin(Email!.PlainValue);
    }
}
