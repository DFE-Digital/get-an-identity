using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

public class EditWebHookModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public EditWebHookModel(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [FromRoute]
    public Guid WebHookId { get; set; }

    [BindProperty]
    [Display(Name = "Endpoint")]
    [Required(ErrorMessage = "Enter an endpoint")]
    public string? Endpoint { get; set; }

    [BindProperty]
    public bool Enabled { get; set; }

    [Display(Name = "Secret")]
    public string? Secret { get; set; }

    [Display(Name = "Regenerate secret")]
    [BindProperty]
    public bool RegenerateSecret { get; set; }

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

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!string.IsNullOrEmpty(Endpoint) &&
            (!Uri.TryCreate(Endpoint!, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")))
        {
            ModelState.AddModelError(nameof(Endpoint), "Enter an absolute HTTP(s) URI");
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
            (RegenerateSecret ? WebHookUpdatedEventChanges.Secret : WebHookUpdatedEventChanges.None);

        webHook.Enabled = Enabled;
        webHook.Endpoint = Endpoint!;

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
                UpdatedByUserId = User.GetUserId()!.Value,
                WebHookId = WebHookId
            });

            await _dbContext.SaveChangesAsync();

            TempData.SetFlashSuccess("Web hook updated");
        }

        return RedirectToPage("EditWebHook", new { webHookId = WebHookId });
    }
}
