using Flurl;
using Flurl.Util;
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

    public string? HandoverMethod { get; set; }

    public async Task OnGet()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        if (_findALostTrnIntegrationHelper.Options.UseNewTrnLookupJourney)
        {
            var nextPage = _linkGenerator.TrnHasTrn();
            HandoverUrl = new Url(nextPage).RemoveQuery();
            HandoverParameters = new Url(nextPage).QueryParams.ToKeyValuePairs().ToDictionary(q => q.Key, q => q.Value.ToString()!);
            HandoverMethod = HttpMethods.Get;
        }
        else
        {
            var (url, parameters) = await _findALostTrnIntegrationHelper.GetHandoverRequest(authenticationState);
            HandoverUrl = url;
            HandoverParameters = parameters.ToDictionary(f => f.Key, f => f.Value.ToString());
            HandoverMethod = HttpMethods.Post;
        }
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
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
