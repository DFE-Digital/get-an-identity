using Flurl;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentityServer.State;

namespace TeacherIdentityServer;

public static class UrlHelperExtensions
{
    public static string Email(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/Email");

    public static string EmailConfirmation(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/EmailConfirmation");

    public static string QualifiedTeacherStart(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/QualifiedTeacher/Index");

    public static string QualifiedTeacherName(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/QualifiedTeacher/Name");

    public static string QualifiedTeacherDateOfBirth(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/QualifiedTeacher/DateOfBirth");

    public static string QualifiedTeacherHaveNino(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/QualifiedTeacher/HaveNino");

    public static string QualifiedTeacherNino(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/QualifiedTeacher/Nino");

    public static string QualifiedTeacherTrn(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/QualifiedTeacher/Trn");

    public static string QualifiedTeacherHaveQts(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/QualifiedTeacher/HaveQts");

    public static string QualifiedTeacherHowQts(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/QualifiedTeacher/HowQts");

    public static string QualifiedTeacherCheckAnswers(this IUrlHelper urlHelper) => urlHelper.PageWithAuthenticationJourneyId("/SignIn/QualifiedTeacher/CheckAnswers");

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
