using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.ModelBinding;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications.WebHooks;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
public class EditWebHookModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly int _webHooksCacheDuration;

    public EditWebHookModel(TeacherIdentityServerDbContext dbContext, IClock clock, IOptions<WebHookOptions> webHookOptions)
    {
        _dbContext = dbContext;
        _clock = clock;
        _webHooksCacheDuration = webHookOptions.Value.WebHooksCacheDurationSeconds;
    }

    [FromRoute]
    public Guid WebHookId { get; set; }

    [BindProperty]
    [Display(Name = "Endpoint")]
    [Required(ErrorMessage = "Enter an endpoint")]
    public string? Endpoint { get; set; }

    [BindProperty]
    public bool Enabled { get; set; }

    [Display(Name = "Which events would you like to trigger this webhook?", Description = "Select all that apply")]
    [ModelBinder(BinderType = typeof(WebHookMessageTypesModelBinder))]
    public WebHookMessageTypes WebHookMessageTypes { get; set; }

    [Display(Name = "Secret")]
    public string? Secret { get; set; }

    [Display(Name = "Regenerate secret")]
    [BindProperty]
    public bool RegenerateSecret { get; set; }

    [BindProperty]
    public bool WithinCache { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var webHook = await _dbContext.WebHooks.SingleOrDefaultAsync(wh => wh.WebHookId == WebHookId);

        if (webHook is null)
        {
            return NotFound();
        }

        Endpoint = webHook.Endpoint;
        Enabled = webHook.Enabled;
        Secret = webHook.Secret;
        WebHookMessageTypes = webHook.WebHookMessageTypes;
        WithinCache = webHook.Updated > _clock.UtcNow.AddSeconds(-_webHooksCacheDuration);

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!string.IsNullOrEmpty(Endpoint) &&
            (!Uri.TryCreate(Endpoint!, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")))
        {
            ModelState.AddModelError(nameof(Endpoint), "Enter an absolute HTTP(s) URI");
        }

        if (WebHookMessageTypes == WebHookMessageTypes.None)
        {
            ModelState.AddModelError(nameof(WebHookMessageTypes), "Select at least one event which will trigger the webhook");
        }

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var webHook = await _dbContext.WebHooks.SingleOrDefaultAsync(wh => wh.WebHookId == WebHookId);
        if (webHook is null)
        {
            return NotFound();
        }

        var changes = WebHookUpdatedEventChanges.None |
            (webHook.Enabled != Enabled ? WebHookUpdatedEventChanges.Enabled : WebHookUpdatedEventChanges.None) |
            (webHook.Endpoint != Endpoint ? WebHookUpdatedEventChanges.Endpoint : WebHookUpdatedEventChanges.None) |
            (webHook.WebHookMessageTypes != WebHookMessageTypes ? WebHookUpdatedEventChanges.WebHookMessageTypes : WebHookUpdatedEventChanges.None) |
            (RegenerateSecret ? WebHookUpdatedEventChanges.Secret : WebHookUpdatedEventChanges.None);

        webHook.Enabled = Enabled;
        webHook.Endpoint = Endpoint!;
        webHook.WebHookMessageTypes = WebHookMessageTypes;
        webHook.Updated = _clock.UtcNow;

        if (RegenerateSecret)
        {
            webHook.Secret = WebHook.GenerateSecret();
        }

        if (changes != WebHookUpdatedEventChanges.None)
        {
            _dbContext.AddEvent(new WebHookUpdatedEvent()
            {
                Changes = changes,
                CreatedUtc = _clock.UtcNow,
                Enabled = Enabled,
                Endpoint = Endpoint!,
                UpdatedByUserId = User.GetUserId(),
                WebHookId = WebHookId,
                WebHookMessageTypes = WebHookMessageTypes
            });

            await _dbContext.SaveChangesAsync();

        }

        return RedirectToPage("EditWebHook", new { webHookId = WebHookId });
    }
}
