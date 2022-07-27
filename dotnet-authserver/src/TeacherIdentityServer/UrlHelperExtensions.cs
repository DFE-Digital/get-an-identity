using Flurl;
using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentityServer;

public static class UrlHelperExtensions
{
    public static string Email(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/Email", "asid");

    public static string EmailConfirmation(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/EmailConfirmation", "asid");

    public static string Name(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/Name", "asid");

    /// <summary>
    /// Generates a link to a Razor page and copies the query parameters specified by <paramref name="propagateQueryParams"/>
    /// from the current request to the generated link.
    /// </summary>
    private static string PageWithQueryParams(
        this IUrlHelper urlHelper,
        string pageName,
        params string[] propagateQueryParams)
    {
        var currentQueryParams = urlHelper.ActionContext.HttpContext.Request.Query;
        var url = new Url(urlHelper.Page(pageName));

        foreach (var q in propagateQueryParams)
        {
            url.SetQueryParam(q, currentQueryParams[q]);
        }

        return url;
    }
}
