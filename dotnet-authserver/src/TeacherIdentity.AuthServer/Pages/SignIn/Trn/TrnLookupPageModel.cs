using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

public abstract class TrnLookupPageModel : PageModel
{
    private readonly TrnLookupHelper _trnLookupHelper;

    protected TrnLookupPageModel(
        IIdentityLinkGenerator linkGenerator,
        TrnLookupHelper trnLookupHelper)
    {
        LinkGenerator = linkGenerator;
        _trnLookupHelper = trnLookupHelper;
    }

    public IIdentityLinkGenerator LinkGenerator { get; }

    protected async Task<IActionResult?> TryFindTrn()
    {
        var authenticationState = HttpContext.GetAuthenticationState();
        var lookupResult = await _trnLookupHelper.LookupTrn(authenticationState);
        return lookupResult is not null ? new RedirectResult(LinkGenerator.TrnCheckAnswers()) : null;
    }
}
