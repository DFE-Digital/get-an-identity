using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using Flurl;
using TeacherIdentity.AuthServer.Infrastructure.Json;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.State;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public class AuthenticationState
{
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
    public IEnumerable<KeyValuePair<string, string>>? AuthorizationResponseParameters { get; set; }
    public string? AuthorizationResponseMode { get; set; }
    public string? RedirectUri { get; }
    public Guid? UserId { get; set; }
    public bool? FirstTimeUser { get; set; }
    public string? EmailAddress { get; set; }
    public bool EmailAddressVerified { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Trn { get; set; }
    public bool HaveCompletedTrnLookup { get; set; }

    /// <summary>
    /// Whether the user has gone back to an earlier page after this journey has been completed.
    /// </summary>
    public bool HaveResumedCompletedJourney { get; set; }

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
            HaveCompletedTrnLookup = GetFirstClaimValue(CustomClaims.HaveCompletedTrnLookup) == bool.TrueString
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

        // trn scope is specified; launch the journey to collect TRN
        if (HasScope(CustomScopes.Trn) && Trn is null && !HaveCompletedTrnLookup)
        {
            return linkGenerator.Trn();
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
        (Trn is not null || HaveCompletedTrnLookup || !HasScope(CustomScopes.Trn)) &&
        UserId.HasValue;

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
                userTypes.Add(UserType.Admin);
                userTypeConstrainedClaims.Add(scope);
            }
        }

        foreach (var scope in CustomScopes.TeacherScopes)
        {
            if (HasScope(scope))
            {
                userTypes.Add(UserType.Teacher);
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
