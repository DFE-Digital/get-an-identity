using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Flurl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Infrastructure.Json;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.State;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public class AuthenticationState
{
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
    {
        Converters =
        {
            new DateOnlyConverter()
        }
    };

    public AuthenticationState(
        Guid journeyId,
        string authorizationUrl)
    {
        JourneyId = journeyId;
        AuthorizationUrl = authorizationUrl;
    }

    public Guid JourneyId { get; }
    public string AuthorizationUrl { get; }
    public IEnumerable<KeyValuePair<string, string>>? AuthorizationResponseParameters { get; set; }
    public string? AuthorizationResponseMode { get; set; }
    public string? RedirectUri { get; set; }
    public Guid? UserId { get; set; }
    public bool? FirstTimeUser { get; set; }
    public string? EmailAddress { get; set; }
    public bool EmailAddressVerified { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Trn { get; set; }
    public bool HaveCompletedTrnLookup { get; set; }

    public static AuthenticationState Deserialize(string serialized) =>
        JsonSerializer.Deserialize<AuthenticationState>(serialized, _jsonSerializerOptions) ??
            throw new ArgumentException($"Serialized {nameof(AuthenticationState)} is not valid.", nameof(serialized));

    public static AuthenticationState FromClaims(
        string authorizationUrl,
        IEnumerable<Claim> claims,
        bool? firstTimeUser = null)
    {
        return new AuthenticationState(journeyId: Guid.NewGuid(), authorizationUrl)
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

        static DateOnly? ParseNullableDate(string? value) => value is not null ? DateOnly.ParseExact(value, DateFormat) : null;

        static Guid? ParseNullableGuid(string? value) => value is not null ? Guid.Parse(value) : null;

        string? GetFirstClaimValue(string claimType) => claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    public OpenIddictRequest GetAuthorizationRequest()
    {
        var parameters = QueryHelpers.ParseQuery(AuthorizationUrl.Split('?')[1]);
        return new OpenIddictRequest(parameters);
    }

    public IEnumerable<Claim> GetClaims()
    {
        if (!IsComplete())
        {
            throw new InvalidOperationException("Cannot retrieve claims until authentication is complete.");
        }

        var authorizationRequest = GetAuthorizationRequest();

        yield return new Claim(Claims.Subject, UserId!.ToString()!);
        yield return new Claim(Claims.Email, EmailAddress!);
        yield return new Claim(Claims.EmailVerified, bool.TrueString);
        yield return new Claim(Claims.Name, FirstName + " " + LastName);
        yield return new Claim(Claims.GivenName, FirstName!);
        yield return new Claim(Claims.FamilyName, LastName!);
        yield return new Claim(Claims.Birthdate, DateOfBirth!.Value.ToString(DateFormat));
        yield return new Claim(CustomClaims.HaveCompletedTrnLookup, HaveCompletedTrnLookup.ToString());

        if (authorizationRequest.HasScope(CustomScopes.Trn) && Trn is not null)
        {
            yield return new Claim(CustomClaims.Trn, Trn);
        }
    }

    public string GetFinalAuthorizationUrl()
    {
        var finalAuthorizationUrl = AuthorizationUrl
            .SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, JourneyId.ToString());

        return finalAuthorizationUrl;
    }

    public string GetNextHopUrl(IUrlHelper urlHelper)
    {
        var request = GetAuthorizationRequest();

        // We need an email address
        if (EmailAddress is null)
        {
            return urlHelper.Email();
        }

        // Email needs to be confirmed with a PIN
        if (!EmailAddressVerified)
        {
            return urlHelper.EmailConfirmation();
        }

        // For now we only support flows that have the trn scope specified
        if (!request.HasScope(CustomScopes.Trn))
        {
            throw new NotSupportedException($"The '{CustomScopes.Trn}' scope must be specified.");
        }

        // trn scope is specified; launch the journey to collect TRN
        if (request.HasScope(CustomScopes.Trn) && Trn is null && !HaveCompletedTrnLookup)
        {
            return urlHelper.Trn();
        }

        // We should have a known user at this point
        Debug.Assert(IsComplete());

        // We're done - complete authorization
        return GetFinalAuthorizationUrl();
    }

    public bool IsComplete() => EmailAddressVerified &&
        (Trn is not null || HaveCompletedTrnLookup || !GetAuthorizationRequest().HasScope(CustomScopes.Trn)) &&
        UserId.HasValue;

    public void Populate(User user, bool firstTimeUser, string? trn)
    {
        UserId = user.UserId;
        EmailAddress = user.EmailAddress;
        EmailAddressVerified = true;
        FirstName = user.FirstName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        HaveCompletedTrnLookup = user.CompletedTrnLookup is not null;
        FirstTimeUser = firstTimeUser;
        Trn = trn;
    }

    public string Serialize() => JsonSerializer.Serialize(this, _jsonSerializerOptions);
}
