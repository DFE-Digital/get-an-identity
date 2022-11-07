using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace TeacherIdentity.AuthServer.ViewComponents;

[ViewComponent(Name = "ClientScopedPartial")]
public class RenderClientScopedPartialViewComponent : ViewComponent
{
    private readonly ClientScopedViewHelper _clientScopedViewHelper;

    public RenderClientScopedPartialViewComponent(ClientScopedViewHelper clientScopedViewHelper)
    {
        _clientScopedViewHelper = clientScopedViewHelper;
    }

    public async Task<IViewComponentResult> InvokeAsync(string viewName)
    {
        if (viewName is null)
        {
            throw new ArgumentNullException(nameof(viewName));
        }

        var view = await _clientScopedViewHelper.FindClientScopedView(viewName);

        if (view is null)
        {
            throw new InvalidOperationException($"Could not find view '{viewName}' for current client or the default fallback.");
        }

        using (var writer = new StringWriter())
        {
            var viewContext = new ViewContext(ViewContext, view, ViewContext.ViewData, writer);
            await view.RenderAsync(viewContext);
            await writer.FlushAsync();

            return new HtmlContentViewComponentResult(new HtmlString(writer.ToString()));
        }
    }
}
