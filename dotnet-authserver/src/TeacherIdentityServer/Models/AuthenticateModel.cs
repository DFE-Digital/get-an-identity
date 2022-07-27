using System.Text.Json;

namespace TeacherIdentityServer.Models;

public class AuthenticateModel
{
    public string? EmailAddress { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public static AuthenticateModel Deserialize(string serialized) =>
        JsonSerializer.Deserialize<AuthenticateModel>(serialized) ??
            throw new ArgumentException($"Serialized {nameof(AuthenticateModel)} is not valid.", nameof(serialized));

    public string Serialize() => JsonSerializer.Serialize(this);
}
