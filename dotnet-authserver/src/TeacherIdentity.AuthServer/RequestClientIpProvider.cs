namespace TeacherIdentity.AuthServer;

public class RequestClientIpProvider : IRequestClientIpProvider
{
    private IHttpContextAccessor _httpContextAccessor { get; }

    public RequestClientIpProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext.");
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? throw new Exception("Connection has no RemoteIpAddress");
        return ip!;
    }
}
