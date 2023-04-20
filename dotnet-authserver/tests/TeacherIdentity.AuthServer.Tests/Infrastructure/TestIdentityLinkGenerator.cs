using System.Diagnostics.CodeAnalysis;
using TeacherIdentity.AuthServer.Helpers;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class TestIdentityLinkGenerator : IdentityLinkGenerator
{
    private readonly AuthenticationState _authenticationState;

    public TestIdentityLinkGenerator(
        AuthenticationState authenticationState,
        LinkGenerator linkGenerator,
        QueryStringSignatureHelper queryStringSignatureHelper)
        : base(linkGenerator, queryStringSignatureHelper)
    {
        _authenticationState = authenticationState;
    }

    protected override bool TryGetAuthenticationState([NotNullWhen(true)] out AuthenticationState? authenticationState)
    {
        authenticationState = _authenticationState;
        return true;
    }
}
