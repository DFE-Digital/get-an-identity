using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Infrastructure.Json;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

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

    public AuthenticationState(
        Guid journeyId,
        string authorizationUrl)
    {
        JourneyId = journeyId;
        AuthorizationUrl = authorizationUrl;
    }

    public Guid JourneyId { get; }
    public string AuthorizationUrl { get; }
    public Guid? UserId { get; set; }
    public bool? FirstTimeUser { get; set; }
    public string? EmailAddress { get; set; }
    public bool EmailAddressVerified { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Trn { get; set; }
    public bool HaveCompletedFindALostTrnJourney { get; set; }

    public static AuthenticationState Deserialize(string serialized) =>
        JsonSerializer.Deserialize<AuthenticationState>(serialized, _jsonSerializerOptions) ??
            throw new ArgumentException($"Serialized {nameof(AuthenticationState)} is not valid.", nameof(serialized));

    public OpenIddictRequest GetAuthorizationRequest()
    {
        var parameters = QueryHelpers.ParseQuery(AuthorizationUrl.Split('?')[1]);
        return new OpenIddictRequest(parameters);
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
        if (request.HasScope(CustomScopes.Trn) && Trn is null && !HaveCompletedFindALostTrnJourney)
        {
            return urlHelper.Trn();
        }

        // We should have a known user at this point
        Debug.Assert(UserId.HasValue);

        // We're done - complete authorization
        return AuthorizationUrl;
    }

    public void Populate(User user, bool firstTimeUser, string? trn)
    {
        UserId = user.UserId;
        EmailAddress = user.EmailAddress;
        EmailAddressVerified = true;
        FirstName = user.FirstName;
        LastName = user.LastName;
        DateOfBirth = user.DateOfBirth;
        HaveCompletedFindALostTrnJourney = true;
        FirstTimeUser = firstTimeUser;
        Trn = trn;
    }

    public string Serialize() => JsonSerializer.Serialize(this, _jsonSerializerOptions);
}
