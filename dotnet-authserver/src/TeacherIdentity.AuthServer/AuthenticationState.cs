using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using TeacherIdentity.AuthServer.Infrastructure.Json;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public class AuthenticationState
{
    private static readonly TimeSpan _journeyLifetime = TimeSpan.FromMinutes(20);

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
    {
        Converters =
        {
            new DateOnlyConverter()
        }
    };

    public AuthenticationState(
        Guid journeyId,
        UserRequirements userRequirements,
        string postSignInUrl,
        DateTime startedAt,
        string? sessionId = null,
        OAuthAuthorizationState? oAuthState = null)
    {
        JourneyId = journeyId;
        UserRequirements = userRequirements;
        PostSignInUrl = postSignInUrl;
        SessionId = sessionId;
        StartedAt = startedAt;
        OAuthState = oAuthState;
    }

    public static TimeSpan AuthCookieLifetime { get; } = TimeSpan.FromMinutes(20);

    public Guid JourneyId { get; }
    public UserRequirements UserRequirements { get; }
    public string PostSignInUrl { get; }
    public string? SessionId { get; }
    public OAuthAuthorizationState? OAuthState { get; }
    [JsonInclude]
    public DateTime StartedAt { get; private set; }
    [JsonInclude]
    public Guid? UserId { get; private set; }
    [JsonInclude]
    public bool? FirstTimeSignInForEmail { get; private set; }
    [JsonInclude]
    public string? EmailAddress { get; private set; }
    [JsonInclude]
    public bool EmailAddressVerified { get; private set; }
    [JsonInclude]
    public string? FirstName { get; private set; }
    [JsonInclude]
    public string? LastName { get; private set; }
    [JsonInclude]
    public bool? HasPreferredName { get; private set; }
    [JsonInclude]
    public string? OfficialFirstName { get; private set; }
    [JsonInclude]
    public string? OfficialLastName { get; private set; }
    [JsonInclude]
    public string? PreviousOfficialFirstName { get; private set; }
    [JsonInclude]
    public string? PreviousOfficialLastName { get; private set; }
    [JsonInclude]
    public DateOnly? DateOfBirth { get; private set; }
    [JsonInclude]
    public string? Trn { get; private set; }
    [JsonInclude]
    public UserType? UserType { get; private set; }
    [JsonInclude]
    public string[]? StaffRoles { get; private set; }
    [JsonInclude]
    public bool HaveCompletedTrnLookup { get; private set; }
    [JsonInclude]
    public TrnLookupState TrnLookup { get; private set; }
    [JsonInclude]
    public string? TrnOwnerEmailAddress { get; private set; }
    [JsonInclude]
    public TrnLookupStatus? TrnLookupStatus { get; private set; }
    [JsonInclude]
    public bool? HasNationalInsuranceNumber { get; private set; }
    [JsonInclude]
    public string? NationalInsuranceNumber { get; private set; }
    [JsonInclude]
    public bool? AwardedQts { get; private set; }
    [JsonInclude]
    public bool? HasIttProvider { get; private set; }
    [JsonInclude]
    public string? IttProviderName { get; private set; }
    [JsonInclude]
    public bool? HasTrn { get; private set; }
    [JsonInclude]
    public string? StatedTrn { get; private set; }
    [JsonInclude]
    public HasPreviousNameOption? HasPreviousName { get; private set; }
    [JsonInclude]
    public string? MobileNumber { get; private set; }
    [JsonInclude]
    public bool MobileNumberVerified { get; private set; }
    [JsonInclude]
    public Guid? ExistingAccountUserId { get; private set; }
    [JsonInclude]
    public string? ExistingAccountEmail { get; private set; }
    [JsonInclude]
    public string? ExistingAccountMobileNumber { get; private set; }
    [JsonInclude]
    public bool? ExistingAccountChosen { get; private set; }

    /// <summary>
    /// Whether the user has gone back to an earlier page after this journey has been completed.
    /// </summary>
    [JsonInclude]
    public bool HaveResumedCompletedJourney { get; private set; }

    public bool EmailAddressSet => EmailAddress is not null;
    public bool MobileNumberSet => MobileNumber is not null;
    public bool HasTrnSet => HasTrn.HasValue;
    public bool PreferredNameSet => HasPreferredName.HasValue;
    public bool OfficialNameSet => OfficialFirstName is not null && OfficialLastName is not null;
    public bool DateOfBirthSet => DateOfBirth.HasValue;
    public bool HasNationalInsuranceNumberSet => HasNationalInsuranceNumber.HasValue;
    public bool NationalInsuranceNumberSet => NationalInsuranceNumber is not null;
    public bool AwardedQtsSet => AwardedQts.HasValue;
    public bool HasIttProviderSet => HasIttProvider.HasValue;

    public static ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "email", nameType: Claims.Name, roleType: Claims.Role);
        var principal = new ClaimsPrincipal(identity);
        return principal;
    }

    public static AuthenticationState Deserialize(string serialized) =>
        JsonSerializer.Deserialize<AuthenticationState>(serialized, _jsonSerializerOptions) ??
            throw new ArgumentException($"Serialized {nameof(AuthenticationState)} is not valid.", nameof(serialized));

    public static AuthenticationState FromUser(
        Guid journeyId,
        UserRequirements userRequirements,
        User? user,
        string postSignInUrl,
        DateTime startedAt,
        string? sessionId = null,
        OAuthAuthorizationState? oAuthState = null,
        bool? firstTimeSignInForEmail = null)
    {
        return new AuthenticationState(journeyId, userRequirements, postSignInUrl, startedAt, sessionId, oAuthState)
        {
            UserId = user?.UserId,
            FirstTimeSignInForEmail = firstTimeSignInForEmail,
            EmailAddress = user?.EmailAddress,
            EmailAddressVerified = user is not null,
            FirstName = user?.FirstName,
            LastName = user?.LastName,
            DateOfBirth = user?.DateOfBirth,
            Trn = user?.Trn,
            HaveCompletedTrnLookup = user?.CompletedTrnLookup is not null,
            TrnLookup = user?.CompletedTrnLookup is not null ? TrnLookupState.Complete : TrnLookupState.None,
            UserType = user?.UserType,
            StaffRoles = user?.StaffRoles,
            TrnLookupStatus = user?.TrnLookupStatus
        };
    }

    [MemberNotNull(nameof(OAuthState))]
    public void EnsureOAuthState()
    {
        if (OAuthState is null)
        {
            throw new InvalidOperationException($"{nameof(OAuthState)} is null.");
        }
    }

    public IEnumerable<Claim> GetInternalClaims()
    {
        if (!IsComplete())
        {
            throw new InvalidOperationException("Cannot retrieve claims until authentication is complete.");
        }

        return UserClaimHelper.GetInternalClaims(this);
    }

    public AuthenticationMilestone GetLastMilestone()
    {
        if (UserId.HasValue)
        {
            return AuthenticationMilestone.Complete;
        }

        if (UserRequirements.HasFlag(UserRequirements.TrnHolder) && TrnLookup != TrnLookupState.None)
        {
            return AuthenticationMilestone.TrnLookupCompleted;
        }

        if (EmailAddressVerified)
        {
            return AuthenticationMilestone.EmailVerified;
        }

        return AuthenticationMilestone.None;
    }

    public string GetNextHopUrl(IIdentityLinkGenerator linkGenerator)
    {
        if (ShouldRedirectToLandingPage())
        {
            return linkGenerator.Landing();
        }

        var milestone = GetLastMilestone();

        if (milestone == AuthenticationMilestone.None)
        {
            return !EmailAddressSet ? linkGenerator.Email() : linkGenerator.EmailConfirmation();
        }

        if (milestone == AuthenticationMilestone.EmailVerified)
        {
            if (UserRequirements.HasFlag(UserRequirements.TrnHolder))
            {
                return linkGenerator.Trn();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        if (milestone == AuthenticationMilestone.TrnLookupCompleted)
        {
            Debug.Assert(TrnLookup != TrnLookupState.None);

            return TrnLookup == TrnLookupState.Complete ? linkGenerator.TrnCheckAnswers() :
                TrnLookup == TrnLookupState.ExistingTrnFound ? linkGenerator.TrnInUse() :
                linkGenerator.TrnInUseChooseEmail();
        }

        Debug.Assert(milestone == AuthenticationMilestone.Complete);
        Debug.Assert(IsComplete());
        return PostSignInUrl;
    }

    public UserType[] GetPermittedUserTypes() => UserRequirements.GetPermittedUserTypes();

    public bool IsComplete() => EmailAddressVerified &&
        (TrnLookup == TrnLookupState.Complete || !UserRequirements.HasFlag(UserRequirements.TrnHolder)) &&
        UserId.HasValue;

    public bool HasExpired(DateTime utcNow) => (StartedAt + _journeyLifetime) <= utcNow;

    public void Reset(DateTime utcNow)
    {
        StartedAt = utcNow;
        UserId = default;
        FirstTimeSignInForEmail = default;
        EmailAddress = default;
        EmailAddressVerified = default;
        FirstName = default;
        LastName = default;
        OfficialFirstName = default;
        OfficialLastName = default;
        PreviousOfficialFirstName = default;
        PreviousOfficialLastName = default;
        DateOfBirth = default;
        Trn = default;
        UserType = default;
        StaffRoles = default;
        HaveCompletedTrnLookup = default;
        TrnLookup = default;
        TrnOwnerEmailAddress = default;
        TrnLookupStatus = default;
        HasNationalInsuranceNumber = default;
        NationalInsuranceNumber = default;
        AwardedQts = default;
        HasIttProvider = default;
        HasTrn = default;
        StatedTrn = default;
        HasPreviousName = default;
    }

    public void OnEmailSet(string email)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.None);

        EmailAddress = email;
        EmailAddressVerified = false;
    }

    public void OnEmailVerified(User? user = null)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.None);

        if (EmailAddress is null)
        {
            throw new InvalidOperationException($"{nameof(EmailAddress)} is not known.");
        }

        EmailAddressVerified = true;
        FirstTimeSignInForEmail = user is null;

        if (user is not null)
        {
            Debug.Assert(user.EmailAddress.Equals(EmailAddress, StringComparison.OrdinalIgnoreCase));

            var permittedUserTypes = GetPermittedUserTypes();
            if (!permittedUserTypes.Contains(user.UserType))
            {
                throw new InvalidOperationException($"Journey does not allow {user.UserType} users.");
            }

            UpdateAuthenticationStateWithUserDetails(user);
        }
    }

    public void OnMobileNumberVerified(User? user = null)
    {
        if (MobileNumber is null)
        {
            throw new InvalidOperationException($"{nameof(MobileNumber)} is not known.");
        }

        MobileNumberVerified = true;

        if (user is not null)
        {
            UpdateAuthenticationStateWithUserDetails(user);
        }
    }

    public void OnTrnLookupCompletedForTrnAlreadyInUse(string existingTrnOwnerEmail)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.EmailVerified);

        if (EmailAddress is null)
        {
            throw new InvalidOperationException($"{nameof(EmailAddress)} is not known.");
        }

        if (!EmailAddressVerified)
        {
            throw new InvalidOperationException($"Email has not been verified.");
        }

        if (TrnLookup != TrnLookupState.None)
        {
            throw new InvalidOperationException($"TRN lookup is invalid: '{TrnLookup}', expected {TrnLookupState.None}.");
        }

        HaveCompletedTrnLookup = true;
        TrnLookup = TrnLookupState.ExistingTrnFound;
        TrnOwnerEmailAddress = existingTrnOwnerEmail;
    }

    public void OnTrnLookupCompletedAndUserRegistered(User user)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.EmailVerified);

        if (EmailAddress is null)
        {
            throw new InvalidOperationException($"{nameof(EmailAddress)} is not known.");
        }

        if (!EmailAddressVerified)
        {
            throw new InvalidOperationException($"Email has not been verified.");
        }

        if (TrnLookup != TrnLookupState.None)
        {
            throw new InvalidOperationException($"TRN lookup is invalid: '{TrnLookup}', expected {TrnLookupState.None}.");
        }

        Debug.Assert(user.CompletedTrnLookup is not null);
        Debug.Assert(user.EmailAddress == EmailAddress);

        var permittedUserTypes = GetPermittedUserTypes();
        if (!permittedUserTypes.Contains(user.UserType))
        {
            throw new InvalidOperationException($"Journey does not allow {user.UserType} users.");
        }

        UserId = user.UserId;
        FirstName = user.FirstName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        HaveCompletedTrnLookup = true;
        FirstTimeSignInForEmail = true;
        Trn = user.Trn;
        TrnLookup = TrnLookupState.Complete;
        UserType = user.UserType;
        StaffRoles = user.StaffRoles;
        TrnLookupStatus = user.TrnLookupStatus;
    }

    public void OnUserRegistered(User user)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.EmailVerified);

        if (EmailAddress is null)
        {
            throw new InvalidOperationException($"{nameof(EmailAddress)} is not known.");
        }

        if (!EmailAddressVerified)
        {
            throw new InvalidOperationException($"Email has not been verified.");
        }

        if (MobileNumber is null)
        {
            throw new InvalidOperationException($"{nameof(MobileNumber)} is not known.");
        }

        if (!MobileNumberSet)
        {
            throw new InvalidOperationException($"Mobile number has not been verified.");
        }

        Debug.Assert(user.EmailAddress == EmailAddress);

        var permittedUserTypes = GetPermittedUserTypes();
        if (!permittedUserTypes.Contains(user.UserType))
        {
            throw new InvalidOperationException($"Journey does not allow {user.UserType} users.");
        }

        UserId = user.UserId;
        FirstName = user.FirstName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        FirstTimeSignInForEmail = true;
        UserType = user.UserType;
        StaffRoles = user.StaffRoles;
    }

    public void OnEmailVerifiedOfExistingAccountForTrn()
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.TrnLookupCompleted);

        if (EmailAddress is null)
        {
            throw new InvalidOperationException($"{nameof(EmailAddress)} is not known.");
        }

        if (!EmailAddressVerified)
        {
            throw new InvalidOperationException($"Email has not been verified.");
        }

        if (TrnLookup != TrnLookupState.ExistingTrnFound)
        {
            throw new InvalidOperationException($"TRN lookup is invalid: '{TrnLookup}', expected {TrnLookupState.ExistingTrnFound}.");
        }

        TrnLookup = TrnLookupState.EmailOfExistingAccountForTrnVerified;
    }

    public void OnEmailAddressChosen(User user)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.TrnLookupCompleted);

        if (EmailAddress is null)
        {
            throw new InvalidOperationException($"{nameof(EmailAddress)} is not known.");
        }

        if (!EmailAddressVerified)
        {
            throw new InvalidOperationException($"Email has not been verified.");
        }

        if (TrnLookup != TrnLookupState.EmailOfExistingAccountForTrnVerified)
        {
            throw new InvalidOperationException($"TRN lookup is invalid: '{TrnLookup}', expected {TrnLookupState.EmailOfExistingAccountForTrnVerified}.");
        }

        Debug.Assert(user.CompletedTrnLookup is not null);

        var permittedUserTypes = GetPermittedUserTypes();
        if (!permittedUserTypes.Contains(user.UserType))
        {
            throw new InvalidOperationException($"Journey does not allow {user.UserType} users.");
        }

        EmailAddress = user.EmailAddress;
        UserId = user.UserId;
        FirstName = user.FirstName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        HaveCompletedTrnLookup = true;
        FirstTimeSignInForEmail = true;  // We want to show the 'first time user' confirmation page, even though this user has signed in before
        Trn = user.Trn;
        TrnLookup = TrnLookupState.Complete;
        UserType = user.UserType;
        StaffRoles = user.StaffRoles;
        TrnLookupStatus = user.TrnLookupStatus;
    }

    public void OnEmailChanged(string email)
    {
        EmailAddress = email;
    }

    public void OnNameSet(string? firstName, string? lastName)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.EmailVerified);

        HasPreferredName = firstName is not null && lastName is not null;
        FirstName = firstName;
        LastName = lastName;
    }

    public void OnNameChanged(string firstName, string lastName)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.Complete);

        FirstName = firstName;
        LastName = lastName;
    }

    public void OnOfficialNameSet(
        string officialFirstName,
        string officialLastName,
        HasPreviousNameOption hasPreviousName,
        string? previousOfficialFirstName,
        string? previousOfficialLastName)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.EmailVerified);

        OfficialFirstName = officialFirstName;
        OfficialLastName = officialLastName;
        HasPreviousName = hasPreviousName;
        PreviousOfficialFirstName = previousOfficialFirstName;
        PreviousOfficialLastName = previousOfficialLastName;
    }

    public void OnDateOfBirthSet(DateOnly dateOfBirth)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.EmailVerified);

        DateOfBirth = dateOfBirth;
    }

    public void OnHasNationalInsuranceNumberSet(bool hasNationalInsuranceNumber)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.EmailVerified);

        if (!hasNationalInsuranceNumber)
        {
            NationalInsuranceNumber = null;
        }

        HasNationalInsuranceNumber = hasNationalInsuranceNumber;
    }

    public void OnNationalInsuranceNumberSet(string nationalInsuranceNumber)
    {
        HasNationalInsuranceNumber = true;
        NationalInsuranceNumber = nationalInsuranceNumber;
    }

    public void OnHasIttProviderSet(bool hasIttProvider, string? ittProviderName)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.EmailVerified);

        if (hasIttProvider && ittProviderName is null)
        {
            throw new ArgumentException($"{nameof(ittProviderName)} must be specified when {nameof(hasIttProvider)} is {true}.");
        }

        HasIttProvider = hasIttProvider;
        IttProviderName = hasIttProvider ? ittProviderName : null;
    }

    public void OnAwardedQtsSet(bool awardedQts)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.EmailVerified);

        if (!awardedQts)
        {
            HasIttProvider = null;
            IttProviderName = null;
        }

        AwardedQts = awardedQts;
    }

    public void OnHasTrnSet(string? trn)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.EmailVerified);

        HasTrn = trn is not null;
        StatedTrn = trn;
    }

    public void OnMobileNumberSet(string mobileNumber)
    {
        MobileNumber = mobileNumber;
        MobileNumberVerified = false;
    }

    public void OnHaveResumedCompletedJourney()
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.Complete);

        HaveResumedCompletedJourney = true;
    }

    public void OnExistingAccountFound(User existingUserAccount)
    {
        ExistingAccountUserId = existingUserAccount.UserId;
        ExistingAccountEmail = existingUserAccount.EmailAddress;
        ExistingAccountMobileNumber = existingUserAccount.MobileNumber;
    }

    public void OnExistingAccountChosen(bool isUsersAccount)
    {
        if (!isUsersAccount)
        {
            ExistingAccountUserId = null;
            ExistingAccountEmail = null;
        }

        ExistingAccountChosen = isUsersAccount;
    }

    public void OnExistingAccountVerified(User user)
    {
        EmailAddressVerified = true;

        UpdateAuthenticationStateWithUserDetails(user);
    }

    public string? GetOfficialName()
    {
        return GetFullName(OfficialFirstName, OfficialLastName);
    }

    public string? GetPreviousOfficialName()
    {
        return GetFullName(PreviousOfficialFirstName, PreviousOfficialLastName);
    }

    public string? GetPreferredName()
    {
        return GetFullName(FirstName, LastName);
    }

    private string? GetFullName(string? firstName, string? lastName)
    {
        return !string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName)
            ? $"{firstName} {lastName}"
            : null;
    }

    public void OnTrnLookupCompleted(string? trn, TrnLookupStatus trnLookupStatus)
    {
        ThrowOnInvalidAuthenticationMilestone(AuthenticationMilestone.EmailVerified, AuthenticationMilestone.TrnLookupCompleted);

        if (trn is not null && trnLookupStatus != AuthServer.TrnLookupStatus.Found)
        {
            throw new ArgumentException($"{nameof(trnLookupStatus)} must be '{AuthServer.TrnLookupStatus.Found} when {nameof(trn)} is not null.");
        }

        if (trn is null && trnLookupStatus == AuthServer.TrnLookupStatus.Found)
        {
            throw new ArgumentException($"{nameof(trnLookupStatus)} cannot be '{AuthServer.TrnLookupStatus.Found} when {nameof(trn)} is null.");
        }

        Trn = trn;
        TrnLookupStatus = trnLookupStatus;
    }

    public string Serialize() => JsonSerializer.Serialize(this, _jsonSerializerOptions);

    public async Task SignIn(HttpContext httpContext)
    {
        if (!IsComplete())
        {
            throw new InvalidOperationException("Journey is not complete.");
        }

        var claims = GetInternalClaims();
        await httpContext.SignInCookies(claims, resetIssued: true, AuthCookieLifetime);
    }

    private void ThrowOnInvalidAuthenticationMilestone(params AuthenticationMilestone[] permittedMilestonse)
    {
        var milestone = GetLastMilestone();

        if (!permittedMilestonse.Contains(milestone))
        {
            throw new InvalidOperationException(
                $"Current milestone '{milestone}' is not permitted (expecting {string.Join(", ", permittedMilestonse.Select(m => $"'{m}'"))}.");
        }
    }

    /// <summary>
    /// Represents a point in the authentication journey that, once completed, cannot be redone.
    /// </summary>
    public enum AuthenticationMilestone
    {
        None = 0,
        EmailVerified = 1,
        TrnLookupCompleted = 100,
        Complete = int.MaxValue
    }

    public enum HasPreviousNameOption
    {
        Yes,
        No,
        PreferNotToSay
    }

    public enum TrnLookupState
    {
        None = 0,
        Complete = 1,
        ExistingTrnFound = 3,
        EmailOfExistingAccountForTrnVerified = 4
    }

    private bool ShouldRedirectToLandingPage()
    {
        if (OAuthState == null) { return false; }

        List<String> ignoredScopes = new List<string>();
        ignoredScopes.AddRange(CustomScopes.StaffUserTypeScopes);
#pragma warning disable CS0618 // Type or member is obsolete
        ignoredScopes.Add(CustomScopes.Trn);
#pragma warning restore CS0618 // Type or member is obsolete
        ignoredScopes.Add(CustomScopes.DqtRead);

        return !OAuthState.HasAnyScope(ignoredScopes) && !UserId.HasValue;
    }

    private void UpdateAuthenticationStateWithUserDetails(User user)
    {
        UserId = user.UserId;
        EmailAddress = user.EmailAddress;
        MobileNumber = user.MobileNumber;
        FirstName = user.FirstName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        HaveCompletedTrnLookup = user.CompletedTrnLookup is not null;
        Trn = user.Trn;
        UserType = user.UserType;
        StaffRoles = user.StaffRoles;
        TrnLookupStatus = user.TrnLookupStatus;

        if (HaveCompletedTrnLookup || Trn is not null)
        {
            TrnLookup = TrnLookupState.Complete;
        }
    }
}

public class OAuthAuthorizationState
{
    public OAuthAuthorizationState(string clientId, string scope, string? redirectUri)
    {
        ClientId = clientId;
        Scope = scope;
        RedirectUri = redirectUri;
    }

    public string ClientId { get; }
    public string Scope { get; }
    [JsonInclude]
    public IEnumerable<KeyValuePair<string, string>>? AuthorizationResponseParameters { get; private set; }
    [JsonInclude]
    public string? AuthorizationResponseMode { get; private set; }
    public string? RedirectUri { get; }

    public string ResolveServiceUrl(Application application)
    {
        var serviceUrl = new Url(application.ServiceUrl ?? "/");

        if (!serviceUrl.IsRelative)
        {
            return serviceUrl;
        }

        if (RedirectUri is null)
        {
            throw new InvalidOperationException($"Cannot resolve a relative {application.ServiceUrl} without a redirect URI.");
        }

        return $"{new Uri(RedirectUri).GetLeftPart(UriPartial.Authority)}/{serviceUrl.ToString().TrimStart('/')}";
    }

    public bool HasScope(string scope) => GetScopes().Contains(scope);

    public bool HasAnyScope(IEnumerable<string> scopes) => GetScopes().Any(scopes.Contains);

    public void SetAuthorizationResponse(
        IEnumerable<KeyValuePair<string, string>> responseParameters,
        string responseMode)
    {
        AuthorizationResponseParameters = responseParameters;
        AuthorizationResponseMode = responseMode;
    }

    private HashSet<string> GetScopes() => new(Scope.Split(' '), StringComparer.OrdinalIgnoreCase);
}
