using Flurl;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class TestIdentityLinkGenerator : IdentityLinkGenerator
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

    protected override string PageWithAuthenticationJourneyId(string pageName, bool authenticationJourneyRequired = true)
    {
        return new Url(_linkGenerator.GetPathByPage(pageName))
            .SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, _authenticationState.JourneyId);
    }
}
