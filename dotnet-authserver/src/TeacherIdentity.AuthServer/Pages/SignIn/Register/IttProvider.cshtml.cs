using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[BindProperties]
[RequiresTrnLookup]
public class IttProvider : PageModel
{
    private readonly IMemoryCache _cache;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IdentityLinkGenerator _linkGenerator;

    public string[]? IttProviderNames;

    public IttProvider(
        IMemoryCache cache,
        IdentityLinkGenerator linkGenerator,
        IDqtApiClient dqtApiClient)
    {
        _cache = cache;
        _linkGenerator = linkGenerator;
        _dqtApiClient = dqtApiClient;
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
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().OnHasIttProviderSet((bool)HasIttProvider!, IttProviderName);

        return Redirect(_linkGenerator.RegisterCheckAnswers());
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.AwardedQtsSet)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterHasQts());
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
}
