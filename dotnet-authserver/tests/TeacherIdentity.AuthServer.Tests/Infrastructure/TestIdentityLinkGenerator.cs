using Flurl;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class TestIdentityLinkGenerator : IdentityLinkGenerator
{
    private readonly AuthenticationState _authenticationState;
    private readonly LinkGenerator _linkGenerator;

    public TestIdentityLinkGenerator(
        QueryStringSignatureHelper queryStringSignatureHelper,
        AuthenticationState authenticationState,
        LinkGenerator linkGenerator)
        : base(queryStringSignatureHelper)
    {
        _authenticationState = authenticationState;
        _linkGenerator = linkGenerator;
    }

    protected override string Page(string pageName, bool authenticationJourneyRequired = true)
    {
        return new Url(_linkGenerator.GetPathByPage(pageName))
            .SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, _authenticationState.JourneyId);
    }
}
