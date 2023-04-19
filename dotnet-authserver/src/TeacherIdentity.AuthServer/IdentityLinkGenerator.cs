using System.Diagnostics.CodeAnalysis;
using Flurl;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer;

public abstract class IdentityLinkGenerator
{
    protected const string DateOfBirthFormat = Infrastructure.ModelBinding.DateOnlyModelBinder.Format;

    protected IdentityLinkGenerator(
        LinkGenerator linkGenerator,
        QueryStringSignatureHelper queryStringSignatureHelper)
    {
        LinkGenerator = linkGenerator;
        QueryStringSignatureHelper = queryStringSignatureHelper;
    }

    protected LinkGenerator LinkGenerator { get; }

    protected QueryStringSignatureHelper QueryStringSignatureHelper { get; }

    protected abstract bool TryGetAuthenticationState([NotNullWhen(true)] out AuthenticationState? authenticationState);

    protected virtual string Page(string pageName, bool authenticationJourneyRequired = true)
    {
        if (!TryGetAuthenticationState(out var authenticationState) && authenticationJourneyRequired)
        {
            throw new InvalidOperationException($"The current request has no {nameof(AuthenticationState)}.");
        }

        var url = new Url(LinkGenerator.GetPathByPage(pageName));

        if (authenticationState is not null)
        {
            url = url.SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authenticationState.JourneyId);
        }

        return url;
    }

    public string CompleteAuthorization() => Page("/SignIn/Complete");

    public string Reset() => Page("/SignIn/Reset");

    public string Email() => Page("/SignIn/Email");

    public string EmailConfirmation() => Page("/SignIn/EmailConfirmation");

    public string ResendEmailConfirmation() => Page("/SignIn/ResendEmailConfirmation");

    public string ResendTrnOwnerEmailConfirmation() => Page("/SignIn/ResendTrnOwnerEmailConfirmation");

    public string Trn() => Page("/SignIn/Trn");

    public string TrnInUse() => Page("/SignIn/TrnInUse");

    public string TrnInUseChooseEmail() => Page("/SignIn/TrnInUseChooseEmail");

    public string TrnInUseCannotAccessEmail() => Page("/SignIn/TrnInUseCannotAccessEmail");

    public string TrnCallback() => Page("/SignIn/TrnCallback");

    public string TrnHasTrn() => Page("/SignIn/Trn/HasTrnPage");

    public string TrnOfficialName() => Page("/SignIn/Trn/OfficialName");

    public string TrnPreferredName() => Page("/SignIn/Trn/PreferredName");

    public string TrnDateOfBirth() => Page("/SignIn/Trn/DateOfBirthPage");

    public string TrnHasNiNumber() => Page("/SignIn/Trn/HasNiNumberPage");

    public string TrnNiNumber() => Page("/SignIn/Trn/NiNumberPage");

    public string TrnAwardedQts() => Page("/SignIn/Trn/AwardedQtsPage");

    public string TrnIttProvider() => Page("/SignIn/Trn/IttProvider");

    public string TrnCheckAnswers() => Page("/SignIn/Trn/CheckAnswers");

    public string TrnNoMatch() => Page("/SignIn/Trn/NoMatch");

    public string Landing() => Page("/SignIn/Landing");

    public string Register() => Page("/SignIn/Register/Index");

    public string RegisterEmail() => Page("/SignIn/Register/Email");

    public string RegisterEmailConfirmation() => Page("/SignIn/Register/EmailConfirmation");

    public string RegisterEmailExists() => Page("/SignIn/Register/EmailExists");

    public string RegisterPhone() => Page("/SignIn/Register/Phone");

    public string RegisterPhoneConfirmation() => Page("/SignIn/Register/PhoneConfirmation");

    public string RegisterResendPhoneConfirmation() => Page("/SignIn/Register/ResendPhoneConfirmation");

    public string RegisterPhoneExists() => Page("/SignIn/Register/PhoneExists");

    public string RegisterName() => Page("/SignIn/Register/Name");

    public string RegisterDateOfBirth() => Page("/SignIn/Register/DateOfBirthPage");

    public string RegisterAccountExists() => Page("/SignIn/Register/AccountExists");

    public string RegisterExistingAccountEmailConfirmation() => Page("/SignIn/Register/ExistingAccountEmailConfirmation");

    public string RegisterResendEmailConfirmation() => Page("/SignIn/Register/ResendEmailConfirmation");

    public string RegisterResendExistingAccountEmail() => Page("/SignIn/Register/ResendExistingAccountEmail");

    public string RegisterExistingAccountPhone() => Page("/SignIn/Register/ExistingAccountPhone");

    public string RegisterResendExistingAccountPhone() => Page("/SignIn/Register/ResendExistingAccountPhone");

    public string RegisterExistingAccountPhoneConfirmation() => Page("/SignIn/Register/ExistingAccountPhoneConfirmation");

    public string RegisterChangeEmailRequest() => Page("/SignIn/Register/ChangeEmailRequest");

    public string RegisterHasNiNumber() => Page("/SignIn/Register/HasNiNumberPage");

    public string RegisterNiNumber() => Page("/SignIn/Register/NiNumberPage");

    public string RegisterHasTrn() => Page("/SignIn/Register/HasTrnPage");

    public string RegisterTrn() => Page("/SignIn/Register/TrnPage");

    public string RegisterHasQts() => Page("/SignIn/Register/HasQtsPage");

    public string RegisterIttProvider() => Page("/SignIn/Register/IttProvider");

    public string RegisterCheckAnswers() => Page("/SignIn/Register/CheckAnswers");

    public string UpdateEmail(string? returnUrl, string? cancelUrl) =>
        Page("/Authenticated/UpdateEmail/Index", authenticationJourneyRequired: false)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl);

    public string UpdateEmailConfirmation(string email, string? returnUrl, string? cancelUrl) =>
        Page("/Authenticated/UpdateEmail/Confirmation", authenticationJourneyRequired: false)
            .SetQueryParam("email", email)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string ResendUpdateEmailConfirmation(string email, string? returnUrl, string? cancelUrl) =>
        Page("/Authenticated/UpdateEmail/ResendConfirmation", authenticationJourneyRequired: false)
            .SetQueryParam("email", email)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string UpdateName(string? returnUrl, string? cancelUrl) =>
        Page("/Authenticated/UpdateName", authenticationJourneyRequired: false)
            .SetQueryParam("returnUrl", returnUrl)
            .SetQueryParam("cancelUrl", cancelUrl);

    public string Account(ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountSignOut() =>
        Page("/Account/SignOut", authenticationJourneyRequired: false);

    public string AccountName(ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/Name/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountNameConfirm(string firstName, string lastName, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/Name/Confirm", authenticationJourneyRequired: false)
            .SetQueryParam("firstName", firstName)
            .SetQueryParam("lastName", lastName)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountDateOfBirth(ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/DateOfBirth/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountDateOfBirthConfirm(DateOnly dateOfBirth, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/DateOfBirth/Confirm", authenticationJourneyRequired: false)
            .SetQueryParam("dateOfBirth", dateOfBirth.ToString(DateOfBirthFormat))
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountEmail(ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/Email/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountEmailResend(string email, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/Email/Resend", authenticationJourneyRequired: false)
            .SetQueryParam("email", email)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountEmailConfirm(string email, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/Email/Confirm", authenticationJourneyRequired: false)
            .SetQueryParam("email", email)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountPhone(ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/Phone/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountPhoneResend(string mobileNumber, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/Phone/Resend", authenticationJourneyRequired: false)
            .SetQueryParam("mobileNumber", mobileNumber)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountPhoneConfirm(string mobileNumber, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/Phone/Confirm", authenticationJourneyRequired: false)
            .SetQueryParam("mobileNumber", mobileNumber)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountOfficialName(ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/OfficialName/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountOfficialNameDetails(string? firstName, string? middleName, string? lastName, string? fileName, string? fileId, bool fromConfirmPage, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/OfficialName/Details", authenticationJourneyRequired: false)
            .SetQueryParam("firstName", firstName)
            .SetQueryParam("middleName", middleName)
            .SetQueryParam("lastName", lastName)
            .SetQueryParam("fileName", fileName)
            .SetQueryParam("fileId", fileId)
            .SetQueryParam("fromConfirmPage", fromConfirmPage)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountOfficialNameEvidence(string firstName, string? middleName, string lastName, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/OfficialName/Evidence", authenticationJourneyRequired: false)
            .SetQueryParam("firstName", firstName)
            .SetQueryParam("middleName", middleName)
            .SetQueryParam("lastName", lastName)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountOfficialNameConfirm(string firstName, string? middleName, string lastName, string fileName, string fileId, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/OfficialName/Confirm", authenticationJourneyRequired: false)
            .SetQueryParam("firstName", firstName)
            .SetQueryParam("middleName", middleName)
            .SetQueryParam("lastName", lastName)
            .SetQueryParam("fileName", fileName)
            .SetQueryParam("fileId", fileId)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountOfficialDateOfBirth(ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/OfficialDateOfBirth/Index", authenticationJourneyRequired: false)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo);

    public string AccountOfficialDateOfBirthDetails(DateOnly? dateOfBirth, string? fileName, string? fileId, bool fromConfirmPage, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/OfficialDateOfBirth/Details", authenticationJourneyRequired: false)
            .SetQueryParam("dateOfBirth", dateOfBirth?.ToString(DateOfBirthFormat))
            .SetQueryParam("fileName", fileName)
            .SetQueryParam("fileId", fileId)
            .SetQueryParam("fromConfirmPage", fromConfirmPage)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountOfficialDateOfBirthEvidence(DateOnly dateOfBirth, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/OfficialDateOfBirth/Evidence", authenticationJourneyRequired: false)
            .SetQueryParam("dateOfBirth", dateOfBirth.ToString(DateOfBirthFormat))
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string AccountOfficialDateOfBirthConfirm(DateOnly dateOfBirth, string fileName, string fileId, ClientRedirectInfo? clientRedirectInfo) =>
        Page("/Account/OfficialDateOfBirth/Confirm", authenticationJourneyRequired: false)
            .SetQueryParam("dateOfBirth", dateOfBirth.ToString(DateOfBirthFormat))
            .SetQueryParam("fileName", fileName)
            .SetQueryParam("fileId", fileId)
            .SetQueryParam(ClientRedirectInfo.QueryParameterName, clientRedirectInfo)
            .AppendQueryStringSignature(QueryStringSignatureHelper);

    public string Cookies() =>
        Page("/Cookies", authenticationJourneyRequired: false);

    public string Privacy() =>
        Page("/Privacy", authenticationJourneyRequired: false);

    public string Accessibility() =>
        Page("/Accessibility", authenticationJourneyRequired: false);
}

public class MvcIdentityLinkGenerator : IdentityLinkGenerator
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MvcIdentityLinkGenerator(
        LinkGenerator linkGenerator,
        QueryStringSignatureHelper queryStringSignatureHelper,
        IHttpContextAccessor httpContextAccessor)
        : base(linkGenerator, queryStringSignatureHelper)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override bool TryGetAuthenticationState([NotNullWhen(true)] out AuthenticationState? authenticationState)
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext.");
        return httpContext.TryGetAuthenticationState(out authenticationState);
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
