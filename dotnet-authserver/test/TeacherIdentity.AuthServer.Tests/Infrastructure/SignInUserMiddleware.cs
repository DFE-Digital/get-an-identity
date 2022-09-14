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
        var firstTimeUser = context.Request.Form["FirstTimeUser"] == bool.TrueString;
        var trn = context.Request.Form["Trn"].FirstOrDefault();

        var dbContext = context.RequestServices.GetRequiredService<TeacherIdentityServerDbContext>();

        var user = await dbContext.Users.SingleAsync(u => u.UserId == userId);
        await context.SignInUser(user, firstTimeUser, !string.IsNullOrEmpty(trn) ? trn : null);
    }
}
