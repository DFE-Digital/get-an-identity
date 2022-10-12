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
        authenticationState.Populate(user, firstTimeSignInForEmail);

        await authenticationState.SignIn(context);
    }
}
