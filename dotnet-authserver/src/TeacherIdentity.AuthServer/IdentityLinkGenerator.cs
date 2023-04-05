using Flurl;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer;

public abstract class IdentityLinkGenerator
{
    protected const string DateOfBirthFormat = Infrastructure.ModelBinding.DateOnlyModelBinder.Format;

    protected IdentityLinkGenerator(QueryStringSignatureHelper queryStringSignatureHelper)
    {
        QueryStringSignatureHelper = queryStringSignatureHelper;
    }

    protected QueryStringSignatureHelper QueryStringSignatureHelper { get; }

    protected abstract string PageWithAuthenticationJourneyId(string pageName, bool authenticationJourneyRequired = true);

    public string CompleteAuthorization() => PageWithAuthenticationJourneyId("/SignIn/Complete");

    public string Reset() => PageWithAuthenticationJourneyId("/SignIn/Reset");

    public string Email() => PageWithAuthenticationJourneyId("/SignIn/Email");

    public string EmailConfirmation() => PageWithAuthenticationJourneyId("/SignIn/EmailConfirmation");

    public string ResendEmailConfirmation() => PageWithAuthenticationJourneyId("/SignIn/ResendEmailConfirmation");

    public string ResendTrnOwnerEmailConfirmation() => PageWithAuthenticationJourneyId("/SignIn/ResendTrnOwnerEmailConfirmation");

    public string Trn() => PageWithAuthenticationJourneyId("/SignIn/Trn");

    public string TrnInUse() => PageWithAuthenticationJourneyId("/SignIn/TrnInUse");

    public string TrnInUseChooseEmail() => PageWithAuthenticationJourneyId("/SignIn/TrnInUseChooseEmail");

    public string TrnInUseCannotAccessEmail() => PageWithAuthenticationJourneyId("/SignIn/TrnInUseCannotAccessEmail");

    public string TrnCallback() => PageWithAuthenticationJourneyId("/SignIn/TrnCallback");

    public string TrnHasTrn() => PageWithAuthenticationJourneyId("/SignIn/Trn/HasTrnPage");

    public string TrnOfficialName() => PageWithAuthenticationJourneyId("/SignIn/Trn/OfficialName");

    public string TrnPreferredName() => PageWithAuthenticationJourneyId("/SignIn/Trn/PreferredName");

    public string TrnDateOfBirth() => PageWithAuthenticationJourneyId("/SignIn/Trn/DateOfBirthPage");

    public string TrnHasNiNumber() => PageWithAuthenticationJourneyId("/SignIn/Trn/HasNiNumberPage");

    public string TrnNiNumber() => PageWithAuthenticationJourneyId("/SignIn/Trn/NiNumberPage");

    public string TrnAwardedQts() => PageWithAuthenticationJourneyId("/SignIn/Trn/AwardedQtsPage");

    public string TrnIttProvider() => PageWithAuthenticationJourneyId("/SignIn/Trn/IttProvider");

    public string TrnCheckAnswers() => PageWithAuthenticationJourneyId("/SignIn/Trn/CheckAnswers");

    public string TrnNoMatch() => PageWithAuthenticationJourneyId("/SignIn/Trn/NoMatch");

    public string Landing() => PageWithAuthenticationJourneyId("/SignIn/Landing");

    public string Register() => PageWithAuthenticationJourneyId("/SignIn/Register/Index");

    public string RegisterEmail() => PageWithAuthenticationJourneyId("/SignIn/Register/Email");

    public string RegisterEmailConfirmation() => PageWithAuthenticationJourneyId("/SignIn/Register/EmailConfirmation");

    public string RegisterEmailExists() => PageWithAuthenticationJourneyId("/SignIn/Register/EmailExists");

    public string RegisterPhone() => PageWithAuthenticationJourneyId("/SignIn/Register/Phone");

    public string RegisterPhoneConfirmation() => PageWithAuthenticationJourneyId("/SignIn/Register/PhoneConfirmation");

    public string RegisterResendPhoneConfirmation() => PageWithAuthenticationJourneyId("/SignIn/Register/ResendPhoneConfirmation");

    public string RegisterPhoneExists() => PageWithAuthenticationJourneyId("/SignIn/Register/PhoneExists");

    public string RegisterName() => PageWithAuthenticationJourneyId("/SignIn/Register/Name");

    public string RegisterDateOfBirth() => PageWithAuthenticationJourneyId("/SignIn/Register/DateOfBirthPage");

    public string RegisterAccountExists() => PageWithAuthenticationJourneyId("/SignIn/Register/AccountExists");

    public string RegisterExistingAccountEmailConfirmation() => PageWithAuthenticationJourneyId("/SignIn/Register/ExistingAccountEmailConfirmation");

    public string RegisterResendEmailConfirmation() => PageWithAuthenticationJourneyId("/SignIn/Register/ResendEmailConfirmation");

    public string RegisterResendExistingAccountEmail() => PageWithAuthenticationJourneyId("/SignIn/Register/ResendExistingAccountEmail");

    public string RegisterExistingAccountPhone() => PageWithAuthenticationJourneyId("/SignIn/Register/ExistingAccountPhone");

    public string RegisterResendExistingAccountPhone() => PageWithAuthenticationJourneyId("/SignIn/Register/ResendExistingAccountPhone");

    public string RegisterExistingAccountPhoneConfirmation() => PageWithAuthenticationJourneyId("/SignIn/Register/ExistingAccountPhoneConfirmation");

    public string RegisterChangeEmailRequest() => PageWithAuthenticationJourneyId("/SignIn/Register/ChangeEmailRequest");

    public string UpdateEmail(string? returnUrl, string? cancelUrl) =>
        PageWithAuthenticationJourneyId("/Authenticated/UpdateEmail/Index", authenticationJourneyRequired: false)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl);

    public string UpdateEmailConfirmation(string email, string? returnUrl, string? cancelUrl) =>
        PageWithAuthenticationJourneyId("/Authenticated/UpdateEmail/Confirmation", authenticationJourneyRequired: false)
            .SetQueryParam("email", email)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string ResendUpdateEmailConfirmation(string email, string? returnUrl, string? cancelUrl) =>
        PageWithAuthenticationJourneyId("/Authenticated/UpdateEmail/ResendConfirmation", authenticationJourneyRequired: false)
            .SetQueryParam("email", email)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string UpdateName(string? returnUrl, string? cancelUrl) =>
        PageWithAuthenticationJourneyId("/Authenticated/UpdateName", authenticationJourneyRequired: false)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl);

    public string Account(ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountName(ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/Name/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountNameConfirm(string firstName, string lastName, ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/Name/Confirm", authenticationJourneyRequired: false)
            .SetQueryParam("firstName", firstName)
            .SetQueryParam("lastName", lastName)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountDateOfBirth(ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/DateOfBirth/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountDateOfBirthConfirm(DateOnly dateOfBirth, ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/DateOfBirth/Confirm", authenticationJourneyRequired: false)
            .SetQueryParam("dateOfBirth", dateOfBirth.ToString(DateOfBirthFormat))
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountEmail(ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/Email/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountEmailResend(string email, ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/Email/Resend", authenticationJourneyRequired: false)
            .SetQueryParam("email", email)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountEmailConfirm(string email, ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/Email/Confirm", authenticationJourneyRequired: false)
            .SetQueryParam("email", email)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountPhone(ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/Phone/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountPhoneResend(string mobileNumber, ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/Phone/Resend", authenticationJourneyRequired: false)
            .SetQueryParam("mobileNumber", mobileNumber)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountPhoneConfirm(string mobileNumber, ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/Phone/Confirm", authenticationJourneyRequired: false)
            .SetQueryParam("mobileNumber", mobileNumber)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountOfficialName(ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/OfficialName/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountOfficialNameDetails(ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/OfficialName/Details", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountOfficialNameEvidence(string firstName, string? middleName, string lastName, ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/OfficialName/Evidence", authenticationJourneyRequired: false)
            .SetQueryParam("firstName", firstName)
            .SetQueryParam("middleName", middleName)
            .SetQueryParam("lastName", lastName)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    // refactor this!
    public string AccountOfficialNameConfirm(string firstName, string? middleName, string lastName, string fileId, string fileName, ClientRedirectInfo? clientRedirectInfo) =>
        PageWithAuthenticationJourneyId("/Account/OfficialName/Confirm", authenticationJourneyRequired: false)
            .SetQueryParam("firstName", firstName)
            .SetQueryParam("middleName", middleName)
            .SetQueryParam("lastName", lastName)
            .SetQueryParam("fileId", fileId)
            .SetQueryParam("fileName", fileName)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string Cookies() =>
        PageWithAuthenticationJourneyId("/Cookies", authenticationJourneyRequired: false);

    public string Privacy() =>
        PageWithAuthenticationJourneyId("/Privacy", authenticationJourneyRequired: false);

    public string Accessibility() =>
        PageWithAuthenticationJourneyId("/Accessibility", authenticationJourneyRequired: false);
}

public class MvcIdentityLinkGenerator : IdentityLinkGenerator
{
    private readonly LinkGenerator _linkGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MvcIdentityLinkGenerator(
        QueryStringSignatureHelper queryStringSignatureHelper,
        LinkGenerator linkGenerator,
        IHttpContextAccessor httpContextAccessor)
        : base(queryStringSignatureHelper)
    {
        _linkGenerator = linkGenerator;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override string PageWithAuthenticationJourneyId(string pageName, bool authenticationJourneyRequired = true)
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

file static class StringExtensions
{
    public static Url AppendQueryStringSignature(
        this Url url,
        QueryStringSignatureHelper queryStringSignatureHelper)
    {
        return queryStringSignatureHelper.AppendSignature(url);
    }
}
