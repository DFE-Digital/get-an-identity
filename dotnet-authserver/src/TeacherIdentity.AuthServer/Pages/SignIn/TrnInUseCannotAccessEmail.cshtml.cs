using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class TrnInUseCannotAccessEmailModel : BaseEmailConfirmationPageModel
{
    private readonly SignInJourney _journey;

    public TrnInUseCannotAccessEmailModel(
        SignInJourney journey,
        IUserVerificationService userVerificationService,
        PinValidator pinValidator)
        : base(userVerificationService, pinValidator)
    {
        _journey = journey;
    }

    public override string Email => _journey.AuthenticationState.TrnOwnerEmailAddress!;

}
