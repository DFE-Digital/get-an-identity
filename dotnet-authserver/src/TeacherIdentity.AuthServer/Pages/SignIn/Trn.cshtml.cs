using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.TrnLookup;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class TrnModel : PageModel
{
    private readonly FindALostTrnIntegrationHelper _findALostTrnIntegrationHelper;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public TrnModel(
        FindALostTrnIntegrationHelper findALostTrnIntegrationHelper,
        IIdentityLinkGenerator linkGenerator)
    {
        _findALostTrnIntegrationHelper = findALostTrnIntegrationHelper;
        _linkGenerator = linkGenerator;
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

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (_findALostTrnIntegrationHelper.Options.UseNewTrnLookupJourney)
        {
            context.Result = new RedirectResult(_linkGenerator.TrnOfficialName());
            return;
        }

        var authenticationState = context.HttpContext.GetAuthenticationState();

        // We expect to have a verified email at this point but we shouldn't have completed the TRN lookup
        if (string.IsNullOrEmpty(authenticationState.EmailAddress) ||
            !authenticationState.EmailAddressVerified ||
            authenticationState.HaveCompletedTrnLookup)
        {
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(_linkGenerator));
        }
    }
}
