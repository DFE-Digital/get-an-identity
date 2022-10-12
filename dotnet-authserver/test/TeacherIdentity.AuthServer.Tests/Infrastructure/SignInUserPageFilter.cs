using Microsoft.AspNetCore.Mvc.Filters;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

/// <summary>
/// Filter to sign in a user if the AuthenticationState has a UserId populated but the user is not currently
/// signed in.
/// </summary>
public class SignInUserPageFilter : IAsyncPageFilter
{
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (context.HttpContext.TryGetAuthenticationState(out var authenticationState) && authenticationState.UserId is Guid userId &&
            context.HttpContext.User.Identity?.IsAuthenticated == false)
        {
            await authenticationState.SignIn(context.HttpContext);
        }

        await next();
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        return Task.CompletedTask;
    }
}
