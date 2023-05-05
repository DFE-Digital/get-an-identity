using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TeacherIdentity.AuthServer.Journeys;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CheckJourneyTypeAttribute : Attribute, IResourceFilter, IOrderedFilter
{
    public CheckJourneyTypeAttribute(params Type[] journeyTypes)
    {
        JourneyTypes = journeyTypes;
    }

    public Type[] JourneyTypes { get; }

    public int Order => int.MinValue;

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (context.HttpContext.TryGetAuthenticationState(out _))
        {
            var signInJourney = context.HttpContext.RequestServices.GetRequiredService<SignInJourney>();
            var signInJourneyType = signInJourney.GetType();

            if (!JourneyTypes.Any(signInJourneyType.IsAssignableTo))
            {
                context.Result = new BadRequestResult();
            }
        }
    }
}
