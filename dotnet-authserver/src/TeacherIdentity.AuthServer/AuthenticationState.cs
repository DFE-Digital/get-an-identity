using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using TeacherIdentity.AuthServer.Infrastructure.Json;
using TeacherIdentity.AuthServer.Models;
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

    /// <summary>
    /// Whether the user has gone back to an earlier page after this journey has been completed.
    /// </summary>
    [JsonInclude]
    public bool HaveResumedCompletedJourney { get; private set; }

    public static ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "email", nameType: Claims.Name, roleType: Claims.Role);
        var principal = new ClaimsPrincipal(identity);
        return principal;
    }

    public static AuthenticationState Deserialize(string serialized) =>
        JsonSerializer.Deserialize<AuthenticationState>(serialized, _jsonSerializerOptions) ??
            throw new ArgumentException($"Serialized {nameof(AuthenticationState)} is not valid.", nameof(serialized));

    public static AuthenticationState FromInternalClaims(
        Guid journeyId,
        UserRequirements userRequirements,
        ClaimsPrincipal principal,
        string postSignInUrl,
        DateTime startedAt,
        string? sessionId = null,
        OAuthAuthorizationState? oAuthState = null,
        bool? firstTimeSignInForEmail = null)
    {
        return new AuthenticationState(journeyId, userRequirements, postSignInUrl, startedAt, sessionId, oAuthState)
        {
            UserId = principal.GetUserId(throwIfMissing: false),
            FirstTimeSignInForEmail = firstTimeSignInForEmail,
            EmailAddress = principal.GetEmailAddress(throwIfMissing: false),
            EmailAddressVerified = principal.GetEmailAddressVerified(throwIfMissing: false) ?? false,
            FirstName = principal.GetFirstName(throwIfMissing: false),
            LastName = principal.GetLastName(throwIfMissing: false),
            DateOfBirth = principal.GetDateOfBirth(throwIfMissing: false),
            Trn = principal.GetTrn(throwIfMissing: false),
            HaveCompletedTrnLookup = principal.GetHaveCompletedTrnLookup(throwIfMissing: false) ?? false,
            TrnLookup = principal.GetHaveCompletedTrnLookup(throwIfMissing: false) == true ? TrnLookupState.Complete : TrnLookupState.None,
            UserType = principal.GetUserType(throwIfMissing: false),
            StaffRoles = principal.GetStaffRoles()
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

    public string GetNextHopUrl(IIdentityLinkGenerator linkGenerator)
    {
        // We need an email address
        if (EmailAddress is null)
        {
            return linkGenerator.Email();
        }

        // Email needs to be confirmed with a PIN
        if (!EmailAddressVerified)
        {
            return linkGenerator.EmailConfirmation();
        }

        if (UserRequirements.HasFlag(UserRequirements.TrnHolder))
        {
            // If it's not been done before, launch the journey to find the TRN
            if (!HaveCompletedTrnLookup)
            {
                return linkGenerator.Trn();
            }

            // TRN is found but another account owns it
            if (TrnLookup == TrnLookupState.ExistingTrnFound)
            {
                return linkGenerator.TrnInUse();
            }

            // TRN is found but another account owns it and user has verified they can access that account
            // Choose which email address to use with the existing account going forward
            if (TrnLookup == TrnLookupState.EmailOfExistingAccountForTrnVerified)
            {
                return linkGenerator.TrnInUseChooseEmail();
            }
        }

        // We should have a known user at this point
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
        DateOfBirth = default;
        Trn = default;
        UserType = default;
        StaffRoles = default;
        HaveCompletedTrnLookup = default;
        TrnLookup = default;
        TrnOwnerEmailAddress = default;
    }

    public void OnEmailSet(string email)
    {
        EmailAddress = email;
        EmailAddressVerified = false;
    }

    public void OnEmailVerified(User? user)
    {
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

            UserId = user.UserId;
            FirstName = user.FirstName;
            LastName = user.LastName;
            DateOfBirth = user.DateOfBirth;
            HaveCompletedTrnLookup = user.CompletedTrnLookup is not null;
            Trn = user.Trn;
            UserType = user.UserType;
            StaffRoles = user.StaffRoles;

            if (HaveCompletedTrnLookup)
            {
                TrnLookup = TrnLookupState.Complete;
            }
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
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        HaveCompletedTrnLookup = true;
        FirstTimeSignInForEmail = true;  // We want to show the 'first time user' confirmation page, even though this user has signed in before
        Trn = user.Trn;
        TrnLookup = TrnLookupState.Complete;
        UserType = user.UserType;
        StaffRoles = user.StaffRoles;
    }

    public void OnEmailChanged(string email)
    {
        EmailAddress = email;
    }

    public void OnNameChanged(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public void OnHaveResumedCompletedJourney()
    {
        if (!IsComplete())
        {
            throw new InvalidOperationException("Journey is not complete.");
        }

        HaveResumedCompletedJourney = true;
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

    public void SetAuthorizationResponse(
        IEnumerable<KeyValuePair<string, string>> responseParameters,
        string responseMode)
    {
        AuthorizationResponseParameters = responseParameters;
        AuthorizationResponseMode = responseMode;
    }
}
