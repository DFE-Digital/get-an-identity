using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Account.Name;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CheckNameChangeIsEnabledAttribute : Attribute, IAsyncPageFilter, IOrderedFilter
{
    public int Order => int.MinValue;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (!await NameChangeEnabled(context.HttpContext))
        {
            context.Result = new BadRequestResult();
            return;
        }

        await next();
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        return Task.CompletedTask;
    }

    private async Task<bool> NameChangeEnabled(HttpContext httpContext)
    {
        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var dqtSynchronizationEnabled = configuration.GetValue("DqtSynchronizationEnabled", false);
        if (dqtSynchronizationEnabled)
        {
            var dbContext = httpContext.RequestServices.GetRequiredService<TeacherIdentityServerDbContext>();
            var user = await dbContext.Users
                .Where(u => u.UserId == httpContext.User.GetUserId())
                .SingleAsync();

            return user.Trn is null;
        }
        else
        {
            return true;
        }
    }
}
