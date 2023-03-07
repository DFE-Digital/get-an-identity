using Flurl;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer;

public interface IIdentityLinkGenerator
{
    string PageWithAuthenticationJourneyId(string pageName, bool authenticationJourneyRequired = true);
}

public class IdentityLinkGenerator : IIdentityLinkGenerator
{
    private readonly LinkGenerator _linkGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityLinkGenerator(LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor)
    {
        _linkGenerator = linkGenerator;
        _httpContextAccessor = httpContextAccessor;
    }

    public string PageWithAuthenticationJourneyId(string pageName, bool authenticationJourneyRequired = true)
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext.");

        AuthenticationState? authenticationState;

        if (authenticationJourneyRequired)
        {
            authenticationState = httpContext.GetAuthenticationState();
        }
        else
        {
            httpContext.TryGetAuthenticationState(out authenticationState);
        }

        var url = new Url(_linkGenerator.GetPathByPage(pageName));

        if (authenticationState is not null)
        {
            url = url.SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authenticationState.JourneyId);
        }

        return url;
    }
}

public static class IdentityLinkGeneratorExtensions
{
    public static string CompleteAuthorization(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Complete");

    public static string Reset(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Reset");

    public static string Email(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Email");

    public static string EmailConfirmation(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/EmailConfirmation");

    public static string ResendEmailConfirmation(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/ResendEmailConfirmation");

    public static string ResendTrnOwnerEmailConfirmation(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/ResendTrnOwnerEmailConfirmation");

    public static string Trn(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn");

    public static string TrnInUse(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/TrnInUse");

    public static string TrnInUseChooseEmail(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/TrnInUseChooseEmail");

    public static string TrnInUseCannotAccessEmail(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/TrnInUseCannotAccessEmail");

    public static string TrnCallback(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/TrnCallback");

    public static string TrnHasTrn(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn/HasTrnPage");

    public static string TrnOfficialName(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn/OfficialName");

    public static string TrnPreferredName(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn/PreferredName");

    public static string TrnDateOfBirth(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn/DateOfBirthPage");

    public static string TrnHasNiNumber(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn/HasNiNumberPage");

    public static string TrnNiNumber(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn/NiNumberPage");

    public static string TrnAwardedQts(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn/AwardedQtsPage");

    public static string TrnIttProvider(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn/IttProvider");

    public static string TrnCheckAnswers(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn/CheckAnswers");

    public static string TrnNoMatch(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn/NoMatch");

    public static string Landing(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Landing");

    public static string Register(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/Index");

    public static string RegisterEmail(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/Email");

    public static string RegisterEmailConfirmation(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/EmailConfirmation");

    public static string RegisterEmailExists(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/EmailExists");

    public static string RegisterPhone(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/Phone");

    public static string RegisterPhoneConfirmation(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/PhoneConfirmation");

    public static string RegisterResendPhoneConfirmation(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/ResendPhoneConfirmation");

    public static string RegisterPhoneExists(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/PhoneExists");

    public static string RegisterName(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/Name");

    public static string RegisterDateOfBirth(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/DateOfBirthPage");

    public static string RegisterCheckAccount(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/CheckAccount");

    public static string RegisterConfirmExistingAccount(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/ConfirmExistingAccount");

    public static string RegisterResendEmailConfirmation(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/ResendEmailConfirmation");

    public static string RegisterResendConfirmExistingAccountEmail(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Register/ResendConfirmExistingAccountEmail");

    public static string UpdateEmail(this IIdentityLinkGenerator linkGenerator, string? returnUrl, string? cancelUrl) =>
        linkGenerator.PageWithAuthenticationJourneyId("/Authenticated/UpdateEmail/Index", authenticationJourneyRequired: false)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl);

    public static string UpdateEmailConfirmation(this IIdentityLinkGenerator linkGenerator, ProtectedString email, string? returnUrl, string? cancelUrl) =>
        linkGenerator.PageWithAuthenticationJourneyId("/Authenticated/UpdateEmail/Confirmation", authenticationJourneyRequired: false)
            .SetQueryParam("email", email.EncryptedValue)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl);

    public static string ResendUpdateEmailConfirmation(this IIdentityLinkGenerator linkGenerator, ProtectedString email, string? returnUrl, string? cancelUrl) =>
        linkGenerator.PageWithAuthenticationJourneyId("/Authenticated/UpdateEmail/ResendConfirmation", authenticationJourneyRequired: false)
            .SetQueryParam("email", email.EncryptedValue)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl);

    public static string UpdateName(this IIdentityLinkGenerator linkGenerator, string? returnUrl, string? cancelUrl) =>
        linkGenerator.PageWithAuthenticationJourneyId("/Authenticated/UpdateName", authenticationJourneyRequired: false)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl);

    public static string Cookies(this IIdentityLinkGenerator linkGenerator) =>
        linkGenerator.PageWithAuthenticationJourneyId("/Cookies", authenticationJourneyRequired: false);

    public static string Privacy(this IIdentityLinkGenerator linkGenerator) =>
        linkGenerator.PageWithAuthenticationJourneyId("/Privacy", authenticationJourneyRequired: false);

    public static string Accessibility(this IIdentityLinkGenerator linkGenerator) =>
        linkGenerator.PageWithAuthenticationJourneyId("/Accessibility", authenticationJourneyRequired: false);
}
