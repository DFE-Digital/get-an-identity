using Flurl;
using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentityServer;

public static class UrlHelperExtensions
{
    public static string Email(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/Email", "asid");

    public static string EmailConfirmation(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/EmailConfirmation", "asid");

    public static string QualifiedTeacherStart(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/QualifiedTeacher/Index", "asid");

    public static string QualifiedTeacherName(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/QualifiedTeacher/Name", "asid");

    public static string QualifiedTeacherDateOfBirth(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/QualifiedTeacher/DateOfBirth", "asid");

    public static string QualifiedTeacherHaveNino(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/QualifiedTeacher/HaveNino", "asid");

    public static string QualifiedTeacherNino(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/QualifiedTeacher/Nino", "asid");

    public static string QualifiedTeacherTrn(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/QualifiedTeacher/Trn", "asid");

    public static string QualifiedTeacherHaveQts(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/QualifiedTeacher/HaveQts", "asid");

    public static string QualifiedTeacherHowQts(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/QualifiedTeacher/HowQts", "asid");

    public static string QualifiedTeacherCheckAnswers(this IUrlHelper urlHelper) => urlHelper.PageWithQueryParams("/SignIn/QualifiedTeacher/CheckAnswers", "asid");

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
