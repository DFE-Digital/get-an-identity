using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.ViewComponents;

[ViewComponent(Name = "ClientScopedPartial")]
public class RenderClientScopedPartialViewComponent : ViewComponent
{
    private readonly ICurrentClientProvider _currentClientProvider;
    private readonly ICompositeViewEngine _viewEngine;

    public RenderClientScopedPartialViewComponent(
        ICurrentClientProvider currentClientProvider,
        ICompositeViewEngine viewEngine)
    {
        _currentClientProvider = currentClientProvider;
        _viewEngine = viewEngine;
    }

    public async Task<IViewComponentResult> InvokeAsync(string viewName)
    {
        if (viewName is null)
        {
            throw new ArgumentNullException(nameof(viewName));
        }

        var client = await _currentClientProvider.GetCurrentClient();

        if (client is null)
        {
            throw new InvalidOperationException("OIDC client could not be retrieved.");
        }

        // By convention, pascal case the client ID to get the view suffix
        // e.g. register-for-npq -> RegisterForNpq
        var viewNameSuffix = ConvertKebabCaseToPascalCase(client.ClientId!);

        // Look for a client-specific view and or the Default fallback
        var fullViewName = $"{viewName}.{viewNameSuffix}";
        var viewResult = _viewEngine.FindView(ViewContext, fullViewName, isMainPage: false);

        var fallbackViewName = $"{viewName}.Default";
        if (!viewResult.Success)
        {
            viewResult = _viewEngine.FindView(ViewContext, fallbackViewName, isMainPage: false);
        }

        if (!viewResult.Success)
        {
            throw new InvalidOperationException($"Could not find view {fullViewName} or {fallbackViewName}.");
        }

        var view = viewResult.View!;

        using (var writer = new StringWriter())
        {
            var viewContext = new ViewContext(ViewContext, view, ViewContext.ViewData, writer);
            await view.RenderAsync(viewContext);
            await writer.FlushAsync();

            return new HtmlContentViewComponentResult(new HtmlString(writer.ToString()));
        }
    }

    private static string ConvertKebabCaseToPascalCase(string value) =>
        string.Concat(value.Split('-').Select(word => word[0..1].ToUpper() + word[1..]));
}
