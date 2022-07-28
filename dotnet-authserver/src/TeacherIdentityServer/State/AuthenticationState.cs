using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using OpenIddict.Abstractions;

namespace TeacherIdentityServer.State;

public class AuthenticationState
{
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
    public string? EmailAddress { get; set; }
    public bool EmailAddressConfirmed { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PreviousFirstName { get; set; }
    public string? PreviousLastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Nino { get; set; }
    public bool? HaveQts { get; set; }
    public bool? DidAUniversityScittOrSchoolAwardQts { get; set; }
    public string? QtsProviderName { get; set; }
    public string? Trn { get; set; }

    public static AuthenticationState Deserialize(string serialized) =>
        JsonSerializer.Deserialize<AuthenticationState>(serialized) ??
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
        if (!EmailAddressConfirmed)
        {
            return urlHelper.EmailConfirmation();
        }

        // For now we only support flows that have the qualified_teacher scope specified
        if (!request.HasScope(CustomScopes.QualifiedTeacher))
        {
            throw new NotSupportedException($"The '{CustomScopes.QualifiedTeacher}' must be specified.");
        }

        // qualified_teacher scope is specified; launch the journey to collect TRN
        if (request.HasScope(CustomScopes.QualifiedTeacher) && Trn is null)
        {
            return urlHelper.QualifiedTeacherStart();
        }

        // We should be signed in at this point - complete authorization
        if (UserId.HasValue)
        {
            return AuthorizationUrl;
        }

        // We should never get here
        throw new Exception("Cannot continue authorization journey.");
    }

    public string Serialize() => JsonSerializer.Serialize(this);
}
