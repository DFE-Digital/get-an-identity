using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public abstract class BaseEmailConfirmationPageModel : BasePinVerificationPageModel
{
    protected BaseEmailConfirmationPageModel(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator)
        : base(userVerificationService, pinValidator)
    {
    }

    public virtual string? Email => HttpContext.GetAuthenticationState().EmailAddress;

    public override Task<PinGenerationResult> GeneratePin()
    {
        return UserVerificationService.GenerateEmailPin(Email!);
    }
}
