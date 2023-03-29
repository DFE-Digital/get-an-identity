using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Helpers;

namespace TeacherIdentity.AuthServer.Infrastructure.Filters;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class VerifyQueryParameterSignatureAttribute : Attribute, IPageFilter
{
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var helper = context.HttpContext.RequestServices.GetRequiredService<QueryStringSignatureHelper>();
        var url = context.HttpContext.Request.GetEncodedPathAndQuery();

        if (!helper.VerifySignature(url))
        {
            context.Result = new BadRequestResult();
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }
}
