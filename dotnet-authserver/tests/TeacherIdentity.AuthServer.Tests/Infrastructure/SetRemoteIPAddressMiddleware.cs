namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class SetRemoteIPAddressMiddleware
{
    private readonly RequestDelegate _next;

    public SetRemoteIPAddressMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Connection.RemoteIpAddress = new System.Net.IPAddress(127001);

        await _next(context);
    }
}
