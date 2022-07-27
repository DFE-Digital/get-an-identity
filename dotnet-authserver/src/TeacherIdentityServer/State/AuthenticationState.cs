using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeacherIdentityServer.State;

public class AuthenticationState
{
    public AuthenticationState(
        Guid id,
        string authorizationUrl)
    {
        Id = id;
        AuthorizationUrl = authorizationUrl;
    }

    public Guid Id { get; }
    public string AuthorizationUrl { get; }
    public string? EmailAddress { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public static AuthenticationState Deserialize(string serialized) =>
        JsonSerializer.Deserialize<AuthenticationState>(serialized) ??
            throw new ArgumentException($"Serialized {nameof(AuthenticationState)} is not valid.", nameof(serialized));

    public string Serialize() => JsonSerializer.Serialize(this);
}
