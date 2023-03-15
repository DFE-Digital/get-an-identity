using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Common;

public abstract class BasePhoneConfirmationPageModel : BasePinVerificationPageModel
{
    protected BasePhoneConfirmationPageModel(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator)
        : base(userVerificationService, pinValidator)
    {
    }

    public virtual string? MobileNumber => HttpContext.GetAuthenticationState().MobileNumber;

    protected override Task<PinGenerationResult> GeneratePin()
    {
        return UserVerificationService.GenerateSmsPin(MobileNumber!);
    }
}
