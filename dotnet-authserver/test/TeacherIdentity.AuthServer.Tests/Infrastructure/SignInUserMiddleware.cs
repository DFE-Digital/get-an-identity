using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class SignInUserMiddleware
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public SignInUserMiddleware(RequestDelegate next, TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Invoke(HttpContext context)
    {
        var userId = Guid.Parse(context.Request.Form["UserId"]);
        var firstTimeUser = context.Request.Form["FirstTimeUser"] == bool.TrueString;
        var trn = context.Request.Form["Trn"].FirstOrDefault();

        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId);
        await context.SignInUser(user, firstTimeUser, !string.IsNullOrEmpty(trn) ? trn : null);
    }
}
