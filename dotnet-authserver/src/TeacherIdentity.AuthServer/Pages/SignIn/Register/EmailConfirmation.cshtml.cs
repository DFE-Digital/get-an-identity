using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup), typeof(TrnTokenSignInJourney))]
[CheckCanAccessStep(CurrentStep)]
public class EmailConfirmationModel : BaseEmailConfirmationPageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.EmailConfirmation;

    private readonly SignInJourney _journey;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly PreventRegistrationOptions _preventRegistrationOptions;
    private readonly ICurrentClientProvider _currentClientProvider;
    private readonly IConfiguration _configuration;

    public EmailConfirmationModel(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        TeacherIdentityServerDbContext dbContext,
        SignInJourney journey,
        IOptions<PreventRegistrationOptions> preventRegistrationOptions,
        ICurrentClientProvider currentClientProvider,
        IConfiguration configuration)
        : base(userVerificationService, pinValidator)
    {
        _dbContext = dbContext;
        _journey = journey;
        _preventRegistrationOptions = preventRegistrationOptions.Value;
        _currentClientProvider = currentClientProvider;
        _configuration = configuration;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    [BindProperty]
    [Display(Name = "Confirmation code")]
    public override string? Code { get; set; }

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

        var pinVerificationFailedReasons = await UserVerificationService.VerifyEmailPin(Email!, Code!);

        if (pinVerificationFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(pinVerificationFailedReasons);
        }

        var user = await _dbContext.Users.Where(u => u.EmailAddress == Email).SingleOrDefaultAsync();

        // prevent user from registering by redirecting user
        //
        // If user does not exist
        // AND
        // We have a redirect setup for the clientid
        // AND
        // Registration Token is either not present or does not match Registration Token
        var whiteListedAccessToken = _configuration.GetValue<string>("RegistrationToken");
        if (user == null && (_journey.AuthenticationState.RegistrationToken is null || !whiteListedAccessToken!.Equals(_journey.AuthenticationState.RegistrationToken, StringComparison.OrdinalIgnoreCase)))
        {
            var application = await _currentClientProvider.GetCurrentClient()!;
            var clientName = application!.ClientId?.ToString();
            var redirect = _preventRegistrationOptions.ClientRedirects.SingleOrDefault(x => x.ClientId.Equals(application!.ClientId!, StringComparison.OrdinalIgnoreCase));

            if (clientName is not null && redirect != null)
            {
                return Redirect(_journey.LinkGenerator.NoAccountRedirectClient());
            }
        }

        return await _journey.OnEmailVerified(user, CurrentStep);

    }
}
