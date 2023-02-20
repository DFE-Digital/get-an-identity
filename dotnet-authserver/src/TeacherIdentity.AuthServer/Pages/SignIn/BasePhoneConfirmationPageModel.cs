using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public abstract class BasePhoneConfirmationPageModel : BasePinVerificationPageModel
{
    protected BasePhoneConfirmationPageModel(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator)
        : base(userVerificationService, pinValidator)
    {
    }

    public string? MobileNumber => HttpContext.GetAuthenticationState().MobileNumber;

    protected override Task<PinGenerationResult> GeneratePin()
    {
        return UserVerificationService.GenerateSmsPin(MobileNumber!);
    }
}
