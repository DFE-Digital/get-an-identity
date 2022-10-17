using System.Security.Cryptography;

namespace TeacherIdentity.AuthServer.Models;

public class WebHook
{
    public static string GenerateSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public required Guid WebHookId { get; init; }
    public required string Endpoint { get; set; }
    public required bool Enabled { get; set; }
    public required string Secret { get; set; }
}
