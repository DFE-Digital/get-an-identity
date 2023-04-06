using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialDateOfBirth;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CheckOfficialDateOfBirthChangeIsEnabledAttribute : Attribute, IAsyncPageFilter, IOrderedFilter
{
    public int Order => int.MinValue;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (!await DateOfBirthChangeEnabled(context.HttpContext))
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

    private async Task<bool> DateOfBirthChangeEnabled(HttpContext httpContext)
    {
        var trn = httpContext.User.GetTrn(false);

        if (trn is null)
        {
            return false;
        }

        var dbContext = httpContext.RequestServices.GetRequiredService<TeacherIdentityServerDbContext>();
        var identityUserDateOfBirth = await dbContext.Users
            .Where(u => u.Trn == trn)
            .Select(u => u.DateOfBirth)
            .SingleAsync();

        var dqtApiClient = httpContext.RequestServices.GetRequiredService<IDqtApiClient>();
        var dqtUser = await dqtApiClient.GetTeacherByTrn(trn) ??
                      throw new Exception($"User with TRN '{trn}' cannot be found in DQT.");

        httpContext.Items["DqtUser"] = dqtUser;
        return !dqtUser.PendingDateOfBirthChange && !dqtUser.DateOfBirth.Equals(identityUserDateOfBirth!.Value);
    }
}
