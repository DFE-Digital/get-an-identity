namespace TeacherIdentity.AuthServer.State;

public class AuthenticationStateMiddleware
{
    public const string IdQueryParameterName = "asid";

    private readonly RequestDelegate _next;
    private readonly IAuthenticationStateProvider _authenticationStateProvider;

    public AuthenticationStateMiddleware(RequestDelegate next, IAuthenticationStateProvider authenticationStateProvider)
    {
        _next = next;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authenticationState = _authenticationStateProvider.GetAuthenticationState(context);

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

            _authenticationStateProvider.SetAuthenticationState(context, authenticationStateFeature.AuthenticationState);
        }
    }
}
