namespace TeacherIdentity.AuthServer;

public interface IRequestClientIpProvider
{
    string GetClientIpAddress();
}
