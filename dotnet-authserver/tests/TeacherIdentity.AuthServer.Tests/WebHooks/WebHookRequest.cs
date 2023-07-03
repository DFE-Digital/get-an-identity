namespace TeacherIdentity.AuthServer.Tests.WebHooks;

public class WebHookRequest
{
    public string? ContentType { get; set; }
    public string? Signature { get; set; }
    public string? Body { get; set; }
}
