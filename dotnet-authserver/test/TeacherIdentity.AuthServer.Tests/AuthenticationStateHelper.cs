using System.Security.Cryptography;
using System.Text;
using TeacherIdentity.AuthServer.State;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests;

public sealed class AuthenticationStateHelper
{
    private readonly Guid _journeyId;
    private readonly TestAuthenticationStateProvider _authenticationStateProvider;
    private readonly IIdentityLinkGenerator _linkGenerator;

    private AuthenticationStateHelper(
        Guid journeyId,
        TestAuthenticationStateProvider authenticationStateProvider,
        IIdentityLinkGenerator linkGenerator)
    {
        _journeyId = journeyId;
        _authenticationStateProvider = authenticationStateProvider;
        _linkGenerator = linkGenerator;
    }

    public static AuthenticationStateHelper Create(
        Action<AuthenticationState>? configureAuthenticationState,
        HostFixture hostFixture,
        string scope)
    {
        var authenticationStateProvider = (TestAuthenticationStateProvider)hostFixture.Services.GetRequiredService<IAuthenticationStateProvider>();

        var journeyId = Guid.NewGuid();

        var codeChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("12345")));

        var client = TestClients.Client1;
        var authorizationUrl = $"/connect/authorize" +
            $"?client_id={client.ClientId}" +
            $"&response_type=code" +
            $"&scope=email%20profile%20" + Uri.EscapeDataString(scope) +
            $"&redirect_uri={Uri.EscapeDataString(client.RedirectUris.First().ToString())}" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            $"&code_challenge_method=S256" +
            $"&response_mode=form_post";

        var authenticationState = new AuthenticationState(journeyId, authorizationUrl);

        configureAuthenticationState?.Invoke(authenticationState);

        authenticationStateProvider.SetAuthenticationState(httpContext: null, authenticationState);

        var linkGenerator = hostFixture.Services.GetRequiredService<LinkGenerator>();
        var identityLinkGenerator = new TestIdentityLinkGenerator(authenticationState, linkGenerator);

        return new AuthenticationStateHelper(journeyId, authenticationStateProvider, identityLinkGenerator);
    }

    public AuthenticationState AuthenticationState => _authenticationStateProvider.GetAuthenticationState(_journeyId)!;

    public string GetNextHopUrl() => AuthenticationState.GetNextHopUrl(_linkGenerator);

    public string ToQueryParam() => $"{AuthenticationStateMiddleware.IdQueryParameterName}={Uri.EscapeDataString(AuthenticationState.JourneyId.ToString())}";
}
