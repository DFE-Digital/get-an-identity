using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests;

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
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId);
        await context.SignInUser(user);
    }
}
