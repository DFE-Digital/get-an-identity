namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class TestRequestClientIpProvider : IRequestClientIpProvider
{
    public string GetClientIpAddress()
    {
        return "127001";
    }
}
