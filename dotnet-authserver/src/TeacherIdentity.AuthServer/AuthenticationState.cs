using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using TeacherIdentity.AuthServer.Infrastructure.Json;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.State;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public class AuthenticationState
{
    public enum TrnLookupState
    {
        None = 0,
        Complete = 1,
        ExistingTrnFound = 3,
        EmailOfExistingAccountForTrnVerified = 4
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
    {
        Converters =
        {
            new DateOnlyConverter()
        }
    };

    public AuthenticationState(Guid journeyId, string initiatingRequestUrl, string clientId, string scope, string? redirectUri)
    {
        JourneyId = journeyId;
        InitiatingRequestUrl = initiatingRequestUrl;
        ClientId = clientId;
        Scope = scope;
        RedirectUri = redirectUri;
    }

    public Guid JourneyId { get; }
    public string InitiatingRequestUrl { get; }
    public string ClientId { get; }
    public string Scope { get; }
    [JsonInclude]
    public IEnumerable<KeyValuePair<string, string>>? AuthorizationResponseParameters { get; private set; }
    [JsonInclude]
    public string? AuthorizationResponseMode { get; private set; }
    public string? RedirectUri { get; }
    [JsonInclude]
    public Guid? UserId { get; private set; }
    [JsonInclude]
    public bool? FirstTimeUser { get; private set; }
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

    public static AuthenticationState Deserialize(string serialized) =>
        JsonSerializer.Deserialize<AuthenticationState>(serialized, _jsonSerializerOptions) ??
            throw new ArgumentException($"Serialized {nameof(AuthenticationState)} is not valid.", nameof(serialized));

    public static AuthenticationState FromInternalClaims(
        IEnumerable<Claim> claims,
        string initiatingRequestUrl,
        string clientId,
        string scope,
        string? redirectUri,
        bool? firstTimeUser = null)
    {
        return new AuthenticationState(journeyId: Guid.NewGuid(), initiatingRequestUrl, clientId, scope, redirectUri)
        {
            UserId = ParseNullableGuid(claims.FirstOrDefault(c => c.Type == Claims.Subject)?.Value),
            FirstTimeUser = firstTimeUser,
            EmailAddress = GetFirstClaimValue(Claims.Email),
            EmailAddressVerified = GetFirstClaimValue(Claims.EmailVerified) == bool.TrueString,
            FirstName = GetFirstClaimValue(Claims.GivenName),
            LastName = GetFirstClaimValue(Claims.FamilyName),
            DateOfBirth = ParseNullableDate(GetFirstClaimValue(Claims.Birthdate)),
            Trn = GetFirstClaimValue(CustomClaims.Trn),
            HaveCompletedTrnLookup = GetFirstClaimValue(CustomClaims.HaveCompletedTrnLookup) == bool.TrueString,
            TrnLookup = GetFirstClaimValue(CustomClaims.HaveCompletedTrnLookup) == bool.TrueString ? TrnLookupState.Complete : TrnLookupState.None
        };

        static DateOnly? ParseNullableDate(string? value) => value is not null ? DateOnly.ParseExact(value, CustomClaims.DateFormat) : null;

        static Guid? ParseNullableGuid(string? value) => value is not null ? Guid.Parse(value) : null;

        string? GetFirstClaimValue(string claimType) => claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    public IEnumerable<Claim> GetInternalClaims()
    {
        if (!IsComplete())
        {
            throw new InvalidOperationException("Cannot retrieve claims until authentication is complete.");
        }

        return Core();

        IEnumerable<Claim> Core()
        {
            yield return new Claim(Claims.Subject, UserId!.ToString()!);
            yield return new Claim(Claims.Email, EmailAddress!);
            yield return new Claim(Claims.EmailVerified, bool.TrueString);
            yield return new Claim(Claims.Name, FirstName + " " + LastName);
            yield return new Claim(Claims.GivenName, FirstName!);
            yield return new Claim(Claims.FamilyName, LastName!);
            yield return new Claim(CustomClaims.HaveCompletedTrnLookup, HaveCompletedTrnLookup.ToString());

            if (DateOfBirth.HasValue)
            {
                yield return new Claim(Claims.Birthdate, DateOfBirth!.Value.ToString(CustomClaims.DateFormat));
            }

            if (Trn is not null)
            {
                yield return new Claim(CustomClaims.Trn, Trn);
            }
        }
    }

    public string GetFinalAuthorizationUrl()
    {
        var finalAuthorizationUrl = new Url(InitiatingRequestUrl)
            .SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, JourneyId.ToString());

        return finalAuthorizationUrl;
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

        if (HasScope(CustomScopes.Trn))
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

        // We're done - complete authorization
        return GetFinalAuthorizationUrl();
    }

    public UserType GetUserType()
    {
        if (!TryGetUserTypeFromScopes(out var userType, out _))
        {
            throw new InvalidOperationException("Scope is not valid.");
        }

        return userType.Value;
    }

    public bool HasScope(string scope) => Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(scope);

    public bool IsComplete() => EmailAddressVerified &&
        (TrnLookup == TrnLookupState.Complete || !HasScope(CustomScopes.Trn)) &&
        UserId.HasValue;

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
        FirstTimeUser = user is null;

        if (user is not null)
        {
            Debug.Assert(user.EmailAddress == EmailAddress);

            UserId = user.UserId;
            FirstName = user.FirstName;
            LastName = user.LastName;
            DateOfBirth = user.DateOfBirth;
            HaveCompletedTrnLookup = user.CompletedTrnLookup is not null;
            Trn = user.Trn;

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

    public void OnTrnLookupCompletedAndUserRegistered(User user, bool firstTimeUser)
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

        UserId = user.UserId;
        FirstName = user.FirstName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        HaveCompletedTrnLookup = true;
        FirstTimeUser = firstTimeUser;
        Trn = user.Trn;
        TrnLookup = TrnLookupState.Complete;
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

        EmailAddress = user.EmailAddress;
        UserId = user.UserId;
        FirstName = user.FirstName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        HaveCompletedTrnLookup = true;
        FirstTimeUser = false;
        Trn = user.Trn;
        TrnLookup = TrnLookupState.Complete;
    }

    public void OnHaveResumedCompletedJourney()
    {
        if (!IsComplete())
        {
            throw new InvalidOperationException("Journey is not complete.");
        }

        HaveResumedCompletedJourney = true;
    }

    [Obsolete("This is for use by tests only.")]
    public void Populate(User user, bool firstTimeUser)
    {
        UserId = user.UserId;
        EmailAddress = user.EmailAddress;
        EmailAddressVerified = true;
        FirstName = user.FirstName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        HaveCompletedTrnLookup = user.CompletedTrnLookup is not null;
        FirstTimeUser = firstTimeUser;
        Trn = user.Trn;
    }

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

    public string Serialize() => JsonSerializer.Serialize(this, _jsonSerializerOptions);

    public void SetAuthorizationResponse(
        IEnumerable<KeyValuePair<string, string>> responseParameters,
        string responseMode)
    {
        if (!IsComplete())
        {
            throw new InvalidOperationException("Journey is not complete.");
        }

        AuthorizationResponseParameters = responseParameters;
        AuthorizationResponseMode = responseMode;
    }

    public bool ValidateScopes([NotNullWhen(false)] out string? errorMessage) => TryGetUserTypeFromScopes(out _, out errorMessage);

    private bool TryGetUserTypeFromScopes(
        [NotNullWhen(true)] out UserType? userType,
        [NotNullWhen(false)] out string? invalidScopeErrorMessage)
    {
        userType = default;
        invalidScopeErrorMessage = default;

        var userTypes = new HashSet<UserType>();
        var userTypeConstrainedClaims = new List<string>();

        foreach (var scope in CustomScopes.AdminScopes)
        {
            if (HasScope(scope))
            {
                userTypes.Add(UserType.Staff);
                userTypeConstrainedClaims.Add(scope);
            }
        }

        foreach (var scope in CustomScopes.TeacherScopes)
        {
            if (HasScope(scope))
            {
                userTypes.Add(UserType.Default);
                userTypeConstrainedClaims.Add(scope);
            }
        }

        if (userTypes.Count == 0 && !HasScope(CustomScopes.Trn))
        {
            invalidScopeErrorMessage = "The trn scope is required.";
            return false;
        }
        else if (userTypes.Count > 1)
        {
            invalidScopeErrorMessage = $"The {string.Join(", ", userTypeConstrainedClaims.OrderBy(sc => sc))} scopes cannot be combined.";
            return false;
        }

        userType = userTypes.Single();
        return true;
    }
}
