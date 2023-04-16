using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TeacherIdentity.AuthServer.Journeys;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CheckCanAccessStepAttribute : Attribute, IPageFilter
{
    public CheckCanAccessStepAttribute(string stepName)
    {
        StepName = stepName;
    }

    public string StepName { get; }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var journey = context.HttpContext.RequestServices.GetRequiredService<SignInJourney>();

        if (!journey.CanAccessStep(StepName))
        {
            context.Result = new RedirectResult(journey.GetLastAccessibleStep());
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }
}
