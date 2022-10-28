namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class TestRequestClientIpProvider : IRequestClientIpProvider
{
    public const string ClientIpAddress = "127001";

    public string GetClientIpAddress() => ClientIpAddress;
}
