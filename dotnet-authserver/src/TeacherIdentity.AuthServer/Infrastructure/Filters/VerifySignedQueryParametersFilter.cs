using System.Reflection;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Pages;

namespace TeacherIdentity.AuthServer.Infrastructure.Filters;

public class VerifySignedQueryParametersFilter : IPageFilter
{
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var queryParametersToCheck = new List<string>();

        foreach (PageBoundPropertyDescriptor parameter in context.ActionDescriptor.BoundProperties)
        {
            if (parameter.Property.GetCustomAttribute<VerifyInSignatureAttribute>() is not null)
            {
                var queryParameterName = parameter.BindingInfo?.BinderModelName ?? parameter.Name;
                queryParametersToCheck.Add(queryParameterName);
            }
        }

        if (queryParametersToCheck.Count > 0)
        {
            var helper = context.HttpContext.RequestServices.GetRequiredService<QueryStringSignatureHelper>();
            var url = context.HttpContext.Request.GetEncodedPathAndQuery();

            if (!helper.VerifySignature(url, queryParametersToCheck.ToArray()))
            {
                context.Result = new BadRequestResult();
            }
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }
}
