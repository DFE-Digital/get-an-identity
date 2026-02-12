using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class NoAccountRedirectClient : PageModel
{
    private readonly ICurrentClientProvider _currentClientProvider;
    private PreventRegistrationOptions _preventRegistrationOptions { get; }
    public string RedirectUrl { get; set; } = string.Empty;

    public NoAccountRedirectClient(
        IOptions<PreventRegistrationOptions> preventRegistrationOptions,
        ICurrentClientProvider currentClientProvider)
    {
        _currentClientProvider = currentClientProvider;
        _preventRegistrationOptions = preventRegistrationOptions.Value;
    }

    public async Task<IActionResult> OnGet()
    {
        var application = await _currentClientProvider.GetCurrentClient()!;
        var redirect = _preventRegistrationOptions.ClientRedirects.SingleOrDefault(x => x.ClientId == application!.Id!);
        if (redirect != null)
        {
            RedirectUrl = redirect.RedirectUri;
        }

        return Page();
    }
}
