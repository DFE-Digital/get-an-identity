namespace TeacherIdentity.AuthServer.Tests.WebHooks;

public record WebHookRequest
{
    public required string ContentType { get; init; }
    public required string Signature { get; init; }
    public required string Body { get; init; }
}
