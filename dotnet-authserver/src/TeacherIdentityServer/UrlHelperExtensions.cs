using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentityServer;

public static class UrlHelperExtensions
{
    public static string Email(this IUrlHelper urlHelper) =>
        urlHelper.Page(
            pageName: "/Email",
            values: new { returnUrl = urlHelper.ActionContext.HttpContext.Request.Query["returnUrl"] })!;

    public static string EmailConfirmation(this IUrlHelper urlHelper) =>
        urlHelper.Page(
            pageName: "/EmailConfirmation",
            values: new { returnUrl = urlHelper.ActionContext.HttpContext.Request.Query["returnUrl"] })!;

    public static string Name(this IUrlHelper urlHelper) =>
        urlHelper.Page(
            pageName: "/Name",
            values: new { returnUrl = urlHelper.ActionContext.HttpContext.Request.Query["returnUrl"] })!;
}
