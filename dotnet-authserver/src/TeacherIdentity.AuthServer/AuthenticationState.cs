using System.Diagnostics;
using System.Text.Json;
using Flurl;
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
        string originalAuthorizationUrl)
    {
        JourneyId = journeyId;
        OriginalAuthorizationUrl = originalAuthorizationUrl;
    }

    public Guid JourneyId { get; }
    public string OriginalAuthorizationUrl { get; }
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

    public OpenIddictRequest GetAuthorizationRequest()
    {
        var parameters = QueryHelpers.ParseQuery(OriginalAuthorizationUrl.Split('?')[1]);
        return new OpenIddictRequest(parameters);
    }

    public string GetFinalAuthorizationUrl()
    {
        var finalAuthorizationUrl = OriginalAuthorizationUrl;

        if (FirstTimeUser!.Value)
        {
            finalAuthorizationUrl = finalAuthorizationUrl.SetQueryParam("ftu", bool.TrueString);
        }

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
