using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
public class IttProvider : TrnLookupPageModel
{
    private readonly IMemoryCache _cache;

    public string[]? IttProviderNames;

    public IttProvider(
        IMemoryCache cache,
        IIdentityLinkGenerator linkGenerator,
        IDqtApiClient dqtApiClient,
        ILogger<TrnLookupPageModel> logger)
        : base(linkGenerator, dqtApiClient, logger)
    {
        _cache = cache;
    }

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

    public async Task<IActionResult> OnPost()
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

        return await TryFindTrn() ?? Redirect(LinkGenerator.TrnCheckAnswers());
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        // We expect to have a verified email and official names at this point but we shouldn't have completed the TRN lookup
        if (string.IsNullOrEmpty(authenticationState.EmailAddress) ||
            !authenticationState.EmailAddressVerified ||
            string.IsNullOrEmpty(authenticationState.GetOfficialName()) ||
            authenticationState.HaveCompletedTrnLookup)
        {
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(LinkGenerator));
            return;
        }

        if (!_cache.TryGetValue("IttProviderNames", out IttProviderNames))
        {
            IttProviderNames = (await DqtApiClient.GetIttProviders()).IttProviders.Select(result => result.ProviderName).ToArray();
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
