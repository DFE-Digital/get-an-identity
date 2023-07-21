namespace TeacherIdentity.AuthServer.State;

public class AuthenticationStateMiddleware
{
    public const string IdQueryParameterName = "asid";

    private readonly RequestDelegate _next;

    public AuthenticationStateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthenticationStateProvider authenticationStateProvider)
    {
        var authenticationState = await authenticationStateProvider.GetAuthenticationState(context);

        if (authenticationState is not null)
        {
            context.Features.Set(new AuthenticationStateFeature(authenticationState));
        }

        await _next(context);

        var authenticationStateFeature = context.Features.Get<AuthenticationStateFeature>();

        if (authenticationStateFeature is not null)
        {
            if (authenticationStateFeature.AuthenticationState.JourneyId == default)
            {
                throw new InvalidOperationException($"{nameof(AuthenticationState)} must have {nameof(AuthenticationState.JourneyId)} set.");
            }

            await authenticationStateProvider.SetAuthenticationState(context, authenticationStateFeature.AuthenticationState);
        }
    }
}
