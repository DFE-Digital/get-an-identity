using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Account.Phone;

public class Confirm : BasePinVerificationPageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly IClock _clock;

    public Confirm(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        TeacherIdentityServerDbContext dbContext,
        IIdentityLinkGenerator linkGenerator,
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

    [FromQuery(Name = "mobileNumber")]
    public ProtectedString? MobileNumber { get; set; }

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

        var smsPinFailedReasons = await UserVerificationService.VerifySmsPin(MobileNumber!.PlainValue, Code!);

        if (smsPinFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(smsPinFailedReasons);
        }

        await UpdateUserPhone(User.GetUserId()!.Value);
        return Redirect(_linkGenerator.Account(ClientRedirectInfo));
    }

    private async Task UpdateUserPhone(Guid userId)
    {
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId);

        var newMobileNumber = MobileNumber!.PlainValue;

        UserUpdatedEventChanges changes = UserUpdatedEventChanges.None;

        if (user.MobileNumber != newMobileNumber)
        {
            changes |= UserUpdatedEventChanges.MobileNumber;
        }

        if (changes != UserUpdatedEventChanges.None)
        {
            user.MobileNumber = newMobileNumber;
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

            TempData.SetFlashSuccess("Your mobile number has been updated");
        }
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (MobileNumber is null)
        {
            context.Result = BadRequest();
        }
    }

    protected override Task<PinGenerationResult> GeneratePin()
    {
        return UserVerificationService.GenerateSmsPin(MobileNumber!.PlainValue);
    }
}
