using TeacherIdentity.AuthServer.State;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests;

public sealed class AuthenticationStateHelper
{
    private readonly Guid _journeyId;
    private readonly TestAuthenticationStateProvider _authenticationStateProvider;

    private AuthenticationStateHelper(
        Guid journeyId,
        TestAuthenticationStateProvider authenticationStateProvider)
    {
        _journeyId = journeyId;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public static AuthenticationStateHelper Create(
        Action<AuthenticationState>? configureAuthenticationState,
        TestAuthenticationStateProvider authenticationStateProvider)
    {
        var journeyId = Guid.NewGuid();

        var client = TestClients.Client1;
        var authorizationUrl = $"/connect/authorize" +
            $"?client_id={client.ClientId}" +
            $"&response_type=code" +
            $"&scope=email%20profile%20trn" +
            $"&redirect_uri={Uri.EscapeDataString(client.RedirectUris.First().ToString())}";

        var authenticationState = new AuthenticationState(journeyId, authorizationUrl);

        configureAuthenticationState?.Invoke(authenticationState);

        authenticationStateProvider.SetAuthenticationState(httpContext: null, authenticationState);

        return new AuthenticationStateHelper(journeyId, authenticationStateProvider);
    }

    public AuthenticationState AuthenticationState => _authenticationStateProvider.GetAuthenticationState(_journeyId)!;

    public string ToQueryParam() => $"{AuthenticationStateMiddleware.IdQueryParameterName}={Uri.EscapeDataString(AuthenticationState.JourneyId.ToString())}";
}
