using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

    public virtual string? MobileNumber => HttpContext.GetAuthenticationState().MobileNumber;

    public override Task<PinGenerationResult> GeneratePin()
    {
        return UserVerificationService.GenerateSmsPin(MobileNumber!);
    }
}
