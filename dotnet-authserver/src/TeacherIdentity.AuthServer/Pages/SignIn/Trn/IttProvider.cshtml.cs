using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
public class IttProvider : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;
    private IDqtApiClient _dqtApiClient;
    private IMemoryCache _cache;

    public IttProvider(
        IIdentityLinkGenerator linkGenerator,
        IDqtApiClient dqtApiClient,
        IMemoryCache cache)
    {
        _linkGenerator = linkGenerator;
        _dqtApiClient = dqtApiClient;
        _cache = cache;
    }

    public string[]? IttProviderNames;

    // Properties are set in the order that they are declared. Because the value of HasIttProvider
    // is used in the conditional RequiredIfTrue attribute, it should be set first.
    [Display(Name = "Did a university, SCITT or school award your QTS?")]
    [Required(ErrorMessage = "Tell us how you were awarded qualified teacher status (QTS)")]
    public bool? HasIttProvider { get; set; }

    [Display(Name = "Where did you get your QTS?", Description = "Your university, SCITT, school or other training provider")]
    [RequiredIfTrue(nameof(HasIttProvider), ErrorMessage = "Enter your university, SCITT, school or other training provider")]
    public string? IttProviderName { get; set; }

    public void OnGet()
    {
        SetDefaultInputValues();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().OnHaveIttProviderSet((bool)HasIttProvider!);

        if (HasIttProvider == true)
        {
            HttpContext.GetAuthenticationState().IttProviderName = IttProviderName;
        }

        return Redirect(_linkGenerator.TrnCheckAnswers());
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        // We expect to have a verified email and official names at this point but we shouldn't have completed the TRN lookup
        if (string.IsNullOrEmpty(authenticationState.EmailAddress) ||
            !authenticationState.EmailAddressVerified ||
            !authenticationState.HasOfficialName() ||
            authenticationState.HaveCompletedTrnLookup)
        {
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(_linkGenerator));
            return;
        }

        if (!_cache.TryGetValue("IttProviderNames", out IttProviderNames))
        {
            IttProviderNames = (await _dqtApiClient.GetIttProviders()).IttProviders.Select(result => result.ProviderName).ToArray();
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            _cache.Set("IttProviderNames", IttProviderNames, cacheEntryOptions);
        }

        await next();
    }

    private void SetDefaultInputValues()
    {
        HasIttProvider ??= HttpContext.GetAuthenticationState().HaveIttProvider;
        IttProviderName ??= HttpContext.GetAuthenticationState().IttProviderName;
    }
}
