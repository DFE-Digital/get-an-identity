using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public class AuthenticationState
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    [JsonConstructor]
    public AuthenticationState(
        Guid journeyId,
        UserRequirements userRequirements,
        string postSignInUrl,
        DateTime startedAt,
        string? sessionId = null,
        OAuthAuthorizationState? oAuthState = null,
        bool? firstTimeSignInForEmail = null)
    {
        JourneyId = journeyId;
        UserRequirements = userRequirements;
        PostSignInUrl = postSignInUrl;
        SessionId = sessionId;
        StartedAt = startedAt;
        OAuthState = oAuthState;
        FirstTimeSignInForEmail = firstTimeSignInForEmail;
    }

    public static TimeSpan AuthCookieLifetime { get; } = TimeSpan.FromHours(1);
    public static TimeSpan JourneyLifetime { get; } = TimeSpan.FromHours(2);

    public Guid JourneyId { get; }
    public UserRequirements UserRequirements { get; }
    public string PostSignInUrl { get; }
    public string? SessionId { get; }
    public OAuthAuthorizationState? OAuthState { get; }
    [JsonInclude]
    public DateTime StartedAt { get; private set; }
    [JsonInclude]
    public List<string> Visited { get; private set; } = new();
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
    public string? MiddleName { get; private set; }
    [JsonInclude]
    public string? LastName { get; private set; }
    [JsonInclude]
    public bool? HasName { get; private set; }
    [JsonInclude]
    public string? DqtFirstName { get; private set; }
    [JsonInclude]
    public string? DqtMiddleName { get; private set; }
    [JsonInclude]
    public string? DqtLastName { get; private set; }
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
    public bool ContinueWithoutMobileNumber { get; private set; }
    [JsonInclude]
    public Guid? ExistingAccountUserId { get; private set; }
    [JsonInclude]
    public string? ExistingAccountEmail { get; private set; }
    [JsonInclude]
    public string? ExistingAccountMobileNumber { get; private set; }
    [JsonInclude]
    public bool? ExistingAccountChosen { get; private set; }
    [JsonInclude]
    public string? TrnToken { get; private set; }
    [JsonInclude]
    public bool IsInstitutionEmail { get; private set; }
    [JsonInclude]
    public bool? InstitutionEmailChosen { get; private set; }
    [JsonInclude]
    public string? PreferredName { get; private set; }

    /// <summary>
    /// Whether the user has gone back to an earlier page after this journey has been completed.
    /// </summary>
    [JsonInclude]
    public bool HaveResumedCompletedJourney { get; private set; }

    /// <summary>
    /// Whether the signed in user requires elevating to the higher TrnVerificationLevel.
    /// </summary>
    /// <remarks>
    /// This should be set when the user is signed in and remain un-changed for the duration of the journey.
    /// The <see cref="TrnVerificationElevationSuccessful"/> property tracks whether elevation has completed, successfully or not.
    /// </remarks>
    [JsonInclude]
    public bool? RequiresTrnVerificationLevelElevation { get; private set; }
    public bool? TrnVerificationElevationSuccessful { get; set; }

    [JsonIgnore]
    public bool EmailAddressSet => EmailAddress is not null;
    [JsonIgnore]
    public bool MobileNumberSet => MobileNumber is not null;
    [JsonIgnore]
    public bool HasTrnSet => HasTrn.HasValue;
    [JsonIgnore]
    public bool NameSet => HasName.HasValue;
    [JsonIgnore]
    public bool DateOfBirthSet => DateOfBirth.HasValue;
    [JsonIgnore]
    public bool ExistingAccountFound => ExistingAccountUserId.HasValue;
    [JsonIgnore]
    public bool HasNationalInsuranceNumberSet => HasNationalInsuranceNumber.HasValue;
    [JsonIgnore]
    public bool NationalInsuranceNumberSet => NationalInsuranceNumber is not null;
    [JsonIgnore]
    public bool AwardedQtsSet => AwardedQts.HasValue;
    [JsonIgnore]
    public bool HasIttProviderSet => HasIttProvider.HasValue;
    [JsonIgnore]
    public bool ContactDetailsVerified => EmailAddressVerified && MobileNumberVerifiedOrSkipped;
    [JsonIgnore]
    public bool MobileNumberVerifiedOrSkipped => MobileNumberVerified || ContinueWithoutMobileNumber;
    [JsonIgnore]
    public bool HasTrnToken => !string.IsNullOrEmpty(TrnToken);
    [JsonIgnore]
    public bool HasValidEmail => !IsInstitutionEmail || InstitutionEmailChosen == true;
    [JsonIgnore]
    public bool PreferredNameSet => !string.IsNullOrEmpty(PreferredName);
    [JsonIgnore]
    public bool HasMiddleName => !string.IsNullOrEmpty(MiddleName);

    public static ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "email", nameType: Claims.Name, roleType: Claims.Role);
        var principal = new ClaimsPrincipal(identity);
        return principal;
    }

    public static AuthenticationState Deserialize(string serialized) =>
        JsonSerializer.Deserialize<AuthenticationState>(serialized, _jsonSerializerOptions) ??
            throw new ArgumentException($"Serialized {nameof(AuthenticationState)} is not valid.", nameof(serialized));

    [MemberNotNull(nameof(OAuthState))]
    public void EnsureOAuthState()
    {
        if (OAuthState is null)
        {
            throw new InvalidOperationException($"{nameof(OAuthState)} is null.");
        }
    }

    public bool TryGetOAuthState([NotNullWhen(true)] out OAuthAuthorizationState? oAuthState)
    {
        oAuthState = OAuthState;
        return OAuthState is not null;
    }

    public IEnumerable<Claim> GetInternalClaims()
    {
        if (!UserId.HasValue)
        {
            throw new InvalidOperationException("User is not signed in.");
        }

        return UserClaimHelper.GetInternalClaims(this);
    }

    public UserType[] GetPermittedUserTypes() => UserRequirements.GetPermittedUserTypes();

    public bool HasExpired(DateTime utcNow) => (StartedAt + JourneyLifetime) <= utcNow;

    public void Reset(DateTime utcNow)
    {
        // Reset all state except Visited which is used for debugging
        StartedAt = utcNow;
        UserId = default;
        FirstTimeSignInForEmail = default;
        EmailAddress = default;
        EmailAddressVerified = default;
        FirstName = default;
        MiddleName = default;
        LastName = default;
        HasName = default;
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
        IttProviderName = default;
        HasTrn = default;
        StatedTrn = default;
        HasPreviousName = default;
        MobileNumber = default;
        MobileNumberVerified = default;
        ContinueWithoutMobileNumber = default;
        ExistingAccountUserId = default;
        ExistingAccountEmail = default;
        ExistingAccountMobileNumber = default;
        ExistingAccountChosen = default;
        TrnToken = default;
        IsInstitutionEmail = default;
        InstitutionEmailChosen = default;
        PreferredName = default;
        HaveResumedCompletedJourney = default;
        RequiresTrnVerificationLevelElevation = default;
        TrnVerificationElevationSuccessful = default;
    }

    public void OnEmailSet(string email, bool isInstitutionEmail = false)
    {
        EmailAddress = email;
        EmailAddressVerified = false;
        IsInstitutionEmail = isInstitutionEmail;
        InstitutionEmailChosen = null;
    }

    public void OnInstitutionalEmailChosen()
    {
        InstitutionEmailChosen = true;
    }

    public void OnEmailVerified(User? user = null)
    {
        if (ExistingAccountChosen == true && user is not null)
        {
            OnExistingAccountVerified(user);
            return;
        }

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
        if (ExistingAccountChosen == true && user is not null)
        {
            OnExistingAccountVerified(user);
            return;
        }

        if (MobileNumber is null)
        {
            throw new InvalidOperationException($"{nameof(MobileNumber)} is not known.");
        }

        MobileNumberVerified = true;
        FirstTimeSignInForEmail = user is null;
        ContinueWithoutMobileNumber = false;

        if (user is not null)
        {
            UpdateAuthenticationStateWithUserDetails(user);
        }
    }

    public void OnTrnLookupCompletedForTrnAlreadyInUse(string existingTrnOwnerEmail)
    {
        if (EmailAddress is null)
        {
            throw new InvalidOperationException($"{nameof(EmailAddress)} is not known.");
        }

        if (!EmailAddressVerified)
        {
            throw new InvalidOperationException($"Email has not been verified.");
        }

        HaveCompletedTrnLookup = true;
        TrnLookup = TrnLookupState.ExistingTrnFound;
        TrnOwnerEmailAddress = existingTrnOwnerEmail;
    }

    public void OnTrnLookupCompletedAndUserRegistered(User user)
    {
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
        MiddleName = user.MiddleName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        HaveCompletedTrnLookup = true;
        FirstTimeSignInForEmail = true;
        Trn = user.Trn;
        TrnLookup = TrnLookupState.Complete;
        RequiresTrnVerificationLevelElevation = false;
        UserType = user.UserType;
        StaffRoles = user.StaffRoles;
        TrnLookupStatus = user.TrnLookupStatus;
    }

    public void OnUserRegistered(User user)
    {
        if (EmailAddress is null)
        {
            throw new InvalidOperationException($"{nameof(EmailAddress)} is not known.");
        }

        if (!EmailAddressVerified)
        {
            throw new InvalidOperationException($"Email has not been verified.");
        }

        if (MobileNumber is null && !ContinueWithoutMobileNumber)
        {
            throw new InvalidOperationException($"{nameof(MobileNumber)} is not known.");
        }

        if (!MobileNumberVerified && !ContinueWithoutMobileNumber)
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
        MiddleName = user.MiddleName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        FirstTimeSignInForEmail = true;
        UserType = user.UserType;
        StaffRoles = user.StaffRoles;
    }

    public void OnEmailVerifiedOfExistingAccountForTrn()
    {
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
        MiddleName = user.MiddleName;
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

    public void OnNameSet(string? firstName, string? middleName, string? lastName)
    {
        HasName = firstName is not null && lastName is not null;
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
    }

    public void OnNameChanged(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public void OnPreferredNameSet(string preferredName)
    {
        PreferredName = preferredName;
    }

    public void OnDateOfBirthSet(DateOnly dateOfBirth)
    {
        DateOfBirth = dateOfBirth;
    }

    public void OnHasNationalInsuranceNumberSet(bool hasNationalInsuranceNumber)
    {
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
        if (hasIttProvider && ittProviderName is null)
        {
            throw new ArgumentException($"{nameof(ittProviderName)} must be specified when {nameof(hasIttProvider)} is {true}.");
        }

        HasIttProvider = hasIttProvider;
        IttProviderName = hasIttProvider ? ittProviderName : null;
    }

    public void OnAwardedQtsSet(bool awardedQts)
    {
        if (!awardedQts)
        {
            HasIttProvider = null;
            IttProviderName = null;
        }

        AwardedQts = awardedQts;
    }

    public void OnHasTrnSet(bool hasTrn)
    {
        if (!hasTrn)
        {
            StatedTrn = null;
        }

        HasTrn = hasTrn;
    }

    public void OnTrnSet(string? trn)
    {
        HasTrn = trn is not null;
        StatedTrn = trn;
    }

    public void OnMobileNumberSet(string mobileNumber)
    {
        MobileNumber = mobileNumber;
        MobileNumberVerified = false;
        ContinueWithoutMobileNumber = false;
    }

    public void OnContinueWithoutMobileNumber()
    {
        MobileNumber = null;
        MobileNumberVerified = false;
        ContinueWithoutMobileNumber = true;
    }

    public void OnHaveResumedCompletedJourney()
    {
        HaveResumedCompletedJourney = true;
    }

    public void OnExistingAccountSearch(User? existingUserAccount)
    {
        ExistingAccountUserId = existingUserAccount?.UserId;
        ExistingAccountEmail = existingUserAccount?.EmailAddress;
        ExistingAccountMobileNumber = existingUserAccount?.MobileNumber;
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
        MobileNumberVerified = true;

        UpdateAuthenticationStateWithUserDetails(user);
    }

    public void OnSignedInUserProvided(User? user)
    {
        UserId = user?.UserId;
        EmailAddress = user?.EmailAddress;
        EmailAddressVerified = user is not null;
        FirstName = user?.FirstName;
        MiddleName = user?.MiddleName;
        LastName = user?.LastName;
        DateOfBirth = user?.DateOfBirth;
        Trn = user?.Trn;
        RequiresTrnVerificationLevelElevation =
            user is not null && TryGetOAuthState(out var oAuthState) && oAuthState.TrnMatchPolicy == TrnMatchPolicy.Strict ?
                user.EffectiveVerificationLevel != TrnVerificationLevel.Medium :
                null;
        HaveCompletedTrnLookup = user?.CompletedTrnLookup is not null;
        TrnLookup = user?.CompletedTrnLookup is not null ? TrnLookupState.Complete : TrnLookupState.None;
        UserType = user?.UserType;
        StaffRoles = user?.StaffRoles;
        TrnLookupStatus = user?.TrnLookupStatus;
    }

    public void OnTrnTokenProvided(EnhancedTrnToken trnToken)
    {
        TrnToken = trnToken.TrnToken;
        Trn = trnToken.Trn;
        RequiresTrnVerificationLevelElevation = false;
        TrnLookupStatus = AuthServer.TrnLookupStatus.Found;
        FirstName ??= trnToken.FirstName;
        MiddleName ??= trnToken.MiddleName;
        LastName ??= trnToken.LastName;
        DateOfBirth ??= trnToken.DateOfBirth;
        EmailAddress = trnToken.Email;
        EmailAddressVerified = true;
    }

    public string? GetName(bool includeMiddleName = true)
    {
        return NameHelper.GetFullName(FirstName, includeMiddleName ? MiddleName : null, LastName);
    }

    public void OnTrnLookupCompleted(FindTeachersResponseResult? findTeachersResult, TrnLookupStatus trnLookupStatus)
    {
        var trn = findTeachersResult?.Trn;

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

        if (RequiresTrnVerificationLevelElevation == true)
        {
            TrnVerificationElevationSuccessful = Trn is not null;
        }

        if (findTeachersResult is not null && !string.IsNullOrEmpty(findTeachersResult.FirstName) && !string.IsNullOrEmpty(findTeachersResult.LastName))
        {
            DqtFirstName = findTeachersResult.FirstName;
            DqtMiddleName = findTeachersResult.MiddleName;
            DqtLastName = findTeachersResult.LastName;
        }
    }

    public string Serialize() => JsonSerializer.Serialize(this, _jsonSerializerOptions);

    public async Task<ClaimsPrincipal> SignIn(HttpContext httpContext)
    {
        var claims = GetInternalClaims();
        return await httpContext.SignInCookies(claims, resetIssued: true, AuthCookieLifetime);
    }

    private void UpdateAuthenticationStateWithUserDetails(User user)
    {
        UserId = user.UserId;
        EmailAddress = user.EmailAddress;
        MobileNumber = user.MobileNumber;
        FirstName = user.FirstName;
        MiddleName = user.MiddleName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        UserType = user.UserType;
        StaffRoles = user.StaffRoles;

        if (!HasTrnToken)
        {
            // If we are in a TRN token sign-in journey we don't want to update these values
            HaveCompletedTrnLookup = user.CompletedTrnLookup is not null;
            Trn = user.Trn;
            TrnLookupStatus = user.TrnLookupStatus;

            if (HaveCompletedTrnLookup || Trn is not null)
            {
                TrnLookup = TrnLookupState.Complete;
                RequiresTrnVerificationLevelElevation =
                    TryGetOAuthState(out var oAuthState) && oAuthState.TrnMatchPolicy == TrnMatchPolicy.Strict &&
                    user.EffectiveVerificationLevel != TrnVerificationLevel.Medium;
            }
        }
        else
        {
            RequiresTrnVerificationLevelElevation = false;
        }
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
    public TrnRequirementType? TrnRequirementType { get; init; }
    public TrnMatchPolicy? TrnMatchPolicy { get; set; }

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

    public void SetAuthorizationResponse(
        IEnumerable<KeyValuePair<string, string>> responseParameters,
        string responseMode)
    {
        AuthorizationResponseParameters = responseParameters;
        AuthorizationResponseMode = responseMode;
    }

    private HashSet<string> GetScopes() => new(Scope.Split(' '), StringComparer.OrdinalIgnoreCase);
}
