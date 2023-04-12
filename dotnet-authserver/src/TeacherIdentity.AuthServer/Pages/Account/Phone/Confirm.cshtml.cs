using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Account.Phone;

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

    [FromQuery(Name = "mobileNumber")]
    public string? MobileNumber { get; set; }

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

        var parsedMobileNumber = Models.MobileNumber.Parse(MobileNumber!);
        var smsPinFailedReasons = await UserVerificationService.VerifySmsPin(parsedMobileNumber, Code!);

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

        UserUpdatedEventChanges changes = UserUpdatedEventChanges.None;

        if (user.MobileNumber != MobileNumber)
        {
            changes |= UserUpdatedEventChanges.MobileNumber;
        }

        if (changes != UserUpdatedEventChanges.None)
        {
            user.MobileNumber = MobileNumber;
            user.NormalizedMobileNumber = Models.MobileNumber.Parse(MobileNumber!);
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
        var parsedMobileNumber = Models.MobileNumber.Parse(MobileNumber!);
        return UserVerificationService.GenerateSmsPin(parsedMobileNumber);
    }
}
