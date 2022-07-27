namespace TeacherIdentityServer.State;

public class AuthenticationStateMiddleware
{
    public const string IdQueryParameterName = "asid";

    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationStateMiddleware> _logger;

    public AuthenticationStateMiddleware(RequestDelegate next, ILogger<AuthenticationStateMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Query.TryGetValue(IdQueryParameterName, out var asidStr) &&
            Guid.TryParse(asidStr, out var asid))
        {
            var sessionKey = GetSessionKey(asid);
            var serializedAuthenticationState = context.Session.GetString(sessionKey);

            if (serializedAuthenticationState is not null)
            {
                try
                {
                    var authenticationState = AuthenticationState.Deserialize(serializedAuthenticationState);
                    context.Features.Set(new AuthenticationStateFeature(authenticationState));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed deserializing {nameof(AuthenticationState)}.");
                }
            }
        }

        await _next(context);

        var authenticationStateFeature = context.Features.Get<AuthenticationStateFeature>();

        if (authenticationStateFeature is not null)
        {
            if (authenticationStateFeature.AuthenticationState.Id == default)
            {
                throw new InvalidOperationException($"{nameof(AuthenticationState)} must have {nameof(AuthenticationState.Id)} set.");
            }

            var sessionKey = GetSessionKey(authenticationStateFeature.AuthenticationState.Id);
            var serializedAuthenticationState = authenticationStateFeature.AuthenticationState.Serialize();
            context.Session.SetString(sessionKey, serializedAuthenticationState);
        }
    }

    private static string GetSessionKey(Guid asid) => $"auth-state:{asid:N}";
}
