using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer;

public class ClientScopedViewHelper
{
    private readonly ICurrentClientProvider _currentClientProvider;
    private readonly ICompositeViewEngine _viewEngine;
    private readonly IActionContextAccessor _actionContextAccessor;

    public ClientScopedViewHelper(
        ICurrentClientProvider currentClientProvider,
        ICompositeViewEngine viewEngine,
        IActionContextAccessor actionContextAccessor)
    {
        _currentClientProvider = currentClientProvider;
        _viewEngine = viewEngine;
        _actionContextAccessor = actionContextAccessor;
    }

    public async Task<IView?> FindClientScopedView(string viewName)
    {
        var client = await _currentClientProvider.GetCurrentClient();
        var actionContext = _actionContextAccessor.ActionContext ?? throw new InvalidOperationException("No current ActionContext.");

        // By convention, pascal case the client ID to get the view suffix
        // e.g. register-for-npq -> RegisterForNpq
        var clientViewName = client is not null ? ConvertKebabCaseToPascalCase(client.ClientId!) : null;

        // Look for a client-specific view and or the Default fallback
        var viewEngineResult = (clientViewName is not null ? FindView(clientViewName) : null) ?? FindView("Default");

        return viewEngineResult?.View;

        static string ConvertKebabCaseToPascalCase(string value) =>
            string.Concat(value.Split('-').Select(word => word[0..1].ToUpper() + word[1..]));

        ViewEngineResult? FindView(string clientViewName)
        {
            var fullViewName = string.Format(viewName, clientViewName);
            var viewResult = _viewEngine.FindView(actionContext, fullViewName, isMainPage: false);
            return viewResult.Success ? viewResult : null;
        }
    }
}
