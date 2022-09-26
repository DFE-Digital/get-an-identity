using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class SignInUserMiddleware
{
    public SignInUserMiddleware(RequestDelegate next)
    {
    }

    public async Task Invoke(HttpContext context)
    {
        var userId = Guid.Parse(context.Request.Form["UserId"]);
        var firstTimeSignInForEmail = context.Request.Form["FirstTimeSignInForEmail"] == bool.TrueString;

        var dbContext = context.RequestServices.GetRequiredService<TeacherIdentityServerDbContext>();

        var user = await dbContext.Users.SingleAsync(u => u.UserId == userId);

        var authenticationState = context.GetAuthenticationState();
#pragma warning disable CS0618 // Type or member is obsolete
        authenticationState.Populate(user, firstTimeSignInForEmail);
#pragma warning restore CS0618 // Type or member is obsolete

        await authenticationState.SignIn(context);
    }
}
