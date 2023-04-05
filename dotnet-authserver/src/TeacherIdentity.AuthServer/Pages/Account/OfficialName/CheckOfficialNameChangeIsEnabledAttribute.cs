using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialName;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CheckOfficialNameChangeIsEnabledAttribute : Attribute, IAsyncPageFilter, IOrderedFilter
{
    public int Order => int.MinValue;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var dqtApiClient = context.HttpContext.RequestServices.GetRequiredService<IDqtApiClient>();
        if (!await OfficialNameChangeEnabled(context.HttpContext, dqtApiClient))
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

    private async Task<bool> OfficialNameChangeEnabled(HttpContext httpContext, IDqtApiClient dqtApiClient)
    {
        var trn = httpContext.User.GetTrn(false);

        if (trn is null)
        {
            return false;
        }

        var dqtUser = await dqtApiClient.GetTeacherByTrn(trn) ??
                      throw new Exception($"User with TRN '{trn}' cannot be found in DQT.");

        httpContext.Items["DqtUser"] = dqtUser;
        return !dqtUser.PendingNameChange;
    }
}