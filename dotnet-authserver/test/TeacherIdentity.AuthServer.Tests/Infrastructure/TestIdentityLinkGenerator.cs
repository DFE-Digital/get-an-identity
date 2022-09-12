using Flurl;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class TestIdentityLinkGenerator : IIdentityLinkGenerator
{
    private readonly AuthenticationState _authenticationState;
    private readonly LinkGenerator _linkGenerator;

    public TestIdentityLinkGenerator(
        AuthenticationState authenticationState,
        LinkGenerator linkGenerator)
    {
        _authenticationState = authenticationState;
        _linkGenerator = linkGenerator;
    }

    public string PageWithAuthenticationJourneyId(string pageName)
    {
        return new Url(_linkGenerator.GetPathByPage(pageName))
            .SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, _authenticationState.JourneyId);
    }
}
