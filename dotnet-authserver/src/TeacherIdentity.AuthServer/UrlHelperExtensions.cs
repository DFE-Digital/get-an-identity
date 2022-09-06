using Flurl;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer;

public static class UrlHelperExtensions
{
    public static string Email(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/Email");

    public static string EmailConfirmation(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/EmailConfirmation");

    public static string ResendEmailConfirmation(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/ResendEmailConfirmation");

    public static string Trn(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/Trn");

    public static string TrnCallback(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/TrnCallback");

    /// <summary>
    /// Generates a link to a Razor page and appends the authentication journey id query parameter.
    /// </summary>
    private static string PageWithAuthenticationJourneyId(
        this IUrlHelper urlHelper,
        string pageName)
    {
        var authenticationState = urlHelper.ActionContext.HttpContext.GetAuthenticationState();

        return new Url(urlHelper.Page(pageName))
            .SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authenticationState.JourneyId);
    }
}
