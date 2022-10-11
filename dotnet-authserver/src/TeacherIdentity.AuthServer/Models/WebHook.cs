namespace TeacherIdentity.AuthServer.Models;

public class WebHook
{
    public required Guid WebHookId { get; init; }
    public required string Endpoint { get; set; }
    public required bool Enabled { get; set; }
}
