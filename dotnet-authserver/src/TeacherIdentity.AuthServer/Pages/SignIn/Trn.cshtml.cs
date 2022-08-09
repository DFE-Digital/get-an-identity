using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class TrnModel : PageModel
{
    private readonly FindALostTrnIntegrationHelper _findALostTrnIntegrationHelper;

    public TrnModel(FindALostTrnIntegrationHelper findALostTrnIntegrationHelper)
    {
        _findALostTrnIntegrationHelper = findALostTrnIntegrationHelper;
    }

    public IReadOnlyDictionary<string, string>? HandoverParameters { get; set; }

    public string? HandoverUrl { get; set; }

    public async Task OnGet()
    {
        var authenticationState = HttpContext.GetAuthenticationState();
        var (url, parameters) = await _findALostTrnIntegrationHelper.GetHandoverRequest(authenticationState);
        HandoverUrl = url;
        HandoverParameters = parameters.ToDictionary(f => f.Key, f => f.Value.ToString());
    }
}
