using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TeacherIdentity.AuthServer.Journeys;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CheckCanAccessStepAttribute : Attribute, IResourceFilter
{
    public CheckCanAccessStepAttribute(string stepName)
    {
        StepName = stepName;
    }

    public string StepName { get; }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var journey = context.HttpContext.RequestServices.GetRequiredService<SignInJourney>();

        if (!journey.CanAccessStep(StepName))
        {
            context.Result = new RedirectResult(journey.GetLastAccessibleStepUrl(StepName));
        }
    }
}
