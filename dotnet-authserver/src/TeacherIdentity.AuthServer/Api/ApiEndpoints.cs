using TeacherIdentity.AuthServer.Oidc.Endpoints;

namespace TeacherIdentity.AuthServer.Api;

public static class ApiEndpoints
{
    public static IEndpointConventionBuilder MapApiEndpoints(this IEndpointRouteBuilder builder)
    {
        return builder.MapGroup("/api")
            .MapTrnLookupEndpoints();
    }
}
