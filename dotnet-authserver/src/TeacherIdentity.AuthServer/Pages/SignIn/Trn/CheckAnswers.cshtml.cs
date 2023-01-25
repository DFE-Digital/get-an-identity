using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

public class CheckAnswers : PageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IEmailVerificationService _emailVerificationService;

    public CheckAnswers(
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IEmailVerificationService emailVerificationService)
    {
        _linkGenerator = linkGenerator;
        _dbContext = dbContext;
        _clock = clock;
        _emailVerificationService = emailVerificationService;
    }

    public string BackLink => (HttpContext.GetAuthenticationState().HaveIttProvider == true)
        ? _linkGenerator.TrnIttProvider()
        : _linkGenerator.TrnAwardedQts();

    public string? EmailAddress => HttpContext.GetAuthenticationState().EmailAddress;
    public string? OfficialName => HttpContext.GetAuthenticationState().GetOfficialName();
    public string? PreviousOfficialName => HttpContext.GetAuthenticationState().GetPreviousOfficialName();
    public string? PreferredName => HttpContext.GetAuthenticationState().GetPreferredName();
    public DateOnly? DateOfBirth => HttpContext.GetAuthenticationState().DateOfBirth;
    public bool? HaveNationalInsuranceNumber => HttpContext.GetAuthenticationState().HaveNationalInsuranceNumber;
    public string? NationalInsuranceNumber => HttpContext.GetAuthenticationState().NationalInsuranceNumber;
    public bool? AwardedQts => HttpContext.GetAuthenticationState().AwardedQts;
    public string? IttProviderName => HttpContext.GetAuthenticationState().IttProviderName;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        var userId = Guid.NewGuid();
        var user = new User()
        {
            CompletedTrnLookup = _clock.UtcNow,
            Created = _clock.UtcNow,
            DateOfBirth = authenticationState.DateOfBirth,
            EmailAddress = authenticationState.EmailAddress!,
            FirstName = string.IsNullOrEmpty(authenticationState.FirstName) ? authenticationState.OfficialFirstName! : authenticationState.FirstName,
            LastName = string.IsNullOrEmpty(authenticationState.LastName) ? authenticationState.OfficialLastName! : authenticationState.LastName,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            Trn = authenticationState.Trn,
            TrnAssociationSource = !string.IsNullOrEmpty(authenticationState.Trn) ? TrnAssociationSource.Lookup : null,
            LastSignedIn = _clock.UtcNow,
            RegisteredWithClientId = authenticationState.OAuthState?.ClientId,
            TrnLookupStatus = !string.IsNullOrEmpty(authenticationState.Trn) ? TrnLookupStatus.Found : TrnLookupStatus.None
        };

        _dbContext.Users.Add(user);

        _dbContext.AddEvent(new Events.UserRegisteredEvent()
        {
            ClientId = authenticationState.OAuthState?.ClientId,
            CreatedUtc = _clock.UtcNow,
            User = user
        });

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException dex) when (dex.IsUniqueIndexViolation("ix_users_trn"))
        {
            // TRN is already linked to an existing account

            var existingUser = await _dbContext.Users.SingleAsync(u => u.Trn == authenticationState.Trn);
            var existingUserEmail = existingUser.EmailAddress;

            authenticationState.OnTrnLookupCompletedForTrnAlreadyInUse(existingUserEmail);

            var pinGenerationResult = await _emailVerificationService.GeneratePin(existingUserEmail);

            if (pinGenerationResult.FailedReasons != PinGenerationFailedReasons.None)
            {
                if (pinGenerationResult.FailedReasons == PinGenerationFailedReasons.RateLimitExceeded)
                {
                    return new ViewResult()
                    {
                        StatusCode = 429,
                        ViewName = "TooManyRequests"
                    };
                }

                throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReasons)}: '{pinGenerationResult.FailedReasons}'.");
            }

            return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
        }

        authenticationState.OnTrnLookupCompletedAndUserRegistered(user);
        await authenticationState.SignIn(HttpContext);

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        // We expect to have a verified email and official names at this point but we shouldn't have completed the TRN lookup
        if (string.IsNullOrEmpty(authenticationState.EmailAddress) ||
            !authenticationState.EmailAddressVerified ||
            string.IsNullOrEmpty(authenticationState.GetOfficialName()) ||
            authenticationState.HaveCompletedTrnLookup)
        {
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(_linkGenerator));
        }
    }
}
