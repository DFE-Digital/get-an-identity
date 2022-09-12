using Flurl;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer;

public interface IIdentityLinkGenerator
{
    string PageWithAuthenticationJourneyId(string pageName);
}

public class IdentityLinkGenerator : IIdentityLinkGenerator
{
    private readonly LinkGenerator _linkGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityLinkGenerator(LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor)
    {
        _linkGenerator = linkGenerator;
        _httpContextAccessor = httpContextAccessor;
    }

    public string PageWithAuthenticationJourneyId(string pageName)
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext.");
        var authenticationState = httpContext.GetAuthenticationState();

        return new Url(_linkGenerator.GetPathByPage(pageName))
            .SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authenticationState.JourneyId);
    }
}

public static class IdentityLinkGeneratorExtensions
{
    public static string CompleteAuthorization(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Complete");

    public static string Email(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Email");

    public static string EmailConfirmation(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/EmailConfirmation");

    public static string ResendEmailConfirmation(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/ResendEmailConfirmation");

    public static string Trn(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/Trn");

    public static string TrnCallback(this IIdentityLinkGenerator linkGenerator) => linkGenerator.PageWithAuthenticationJourneyId("/SignIn/TrnCallback");
}
