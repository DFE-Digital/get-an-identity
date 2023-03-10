using Flurl;
using Flurl.Util;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.EmailVerified)]
public class TrnModel : PageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;

    public TrnModel(IIdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public IReadOnlyDictionary<string, string>? HandoverParameters { get; set; }

    public string? HandoverUrl { get; set; }

    public string? HandoverMethod { get; set; }

    public void OnGet()
    {
        var nextPage = _linkGenerator.TrnHasTrn();
        HandoverUrl = new Url(nextPage).RemoveQuery();
        HandoverParameters = new Url(nextPage).QueryParams.ToKeyValuePairs().ToDictionary(q => q.Key, q => q.Value.ToString()!);
        HandoverMethod = HttpMethods.Get;
    }
}
