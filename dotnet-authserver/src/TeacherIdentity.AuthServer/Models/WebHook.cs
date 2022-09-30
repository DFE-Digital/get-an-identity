namespace TeacherIdentity.AuthServer.Models;

public class WebHook
{
    public int WebHookId { get; set; }
    public required string Endpoint { get; set; }
    public required bool Enabled { get; set; }
}
