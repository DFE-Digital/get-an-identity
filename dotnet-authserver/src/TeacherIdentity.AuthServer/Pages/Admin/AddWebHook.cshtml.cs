using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.ModelBinding;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
public class AddWebHookModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public AddWebHookModel(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [BindProperty]
    [Display(Name = "Endpoint")]
    [Required(ErrorMessage = "Enter an endpoint")]
    public string? Endpoint { get; set; }

    [Display(Name = "Which events would you like to trigger this webhook?", Description = "Select all that apply")]
    [ModelBinder(BinderType = typeof(WebHookMessageTypesModelBinder))]
    public WebHookMessageTypes WebHookMessageTypes { get; set; }

    [BindProperty]
    public bool Enabled { get; set; }

    public void OnGet()
    {
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

        var webHookId = Guid.NewGuid();

        _dbContext.WebHooks.Add(new WebHook()
        {
            WebHookId = webHookId,
            Enabled = Enabled,
            Endpoint = Endpoint!,
            Secret = WebHook.GenerateSecret(),
            WebHookMessageTypes = WebHookMessageTypes
        });

        _dbContext.AddEvent(new WebHookAddedEvent()
        {
            AddedByUserId = User.GetUserId(),
            CreatedUtc = _clock.UtcNow,
            Enabled = Enabled,
            Endpoint = Endpoint!,
            WebHookId = webHookId,
            WebHookMessageTypes = WebHookMessageTypes
        });

        await _dbContext.SaveChangesAsync();

        TempData.SetFlashSuccess("Web hook added");
        return RedirectToPage("EditWebHook", new { webHookId = webHookId });
    }
}
