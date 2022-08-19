using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TeacherIdentity.AuthServer.State;

public class RequireAuthenticationStateFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.Features.Get<AuthenticationStateFeature>() is null)
        {
            context.Result = new BadRequestResult();
        }
    }
}
