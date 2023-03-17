using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Common;

public abstract class BaseEmailConfirmationPageModel : BasePinVerificationPageModel
{
    protected BaseEmailConfirmationPageModel(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator)
        : base(userVerificationService, pinValidator)
    {
    }

    public virtual string? Email => HttpContext.GetAuthenticationState().EmailAddress;

    protected override Task<PinGenerationResult> GeneratePin()
    {
        return UserVerificationService.GenerateEmailPin(Email!);
    }
}
