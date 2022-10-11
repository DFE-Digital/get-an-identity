using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

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

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var webHookId = Guid.NewGuid();

        _dbContext.WebHooks.Add(new WebHook()
        {
            WebHookId = webHookId,
            Enabled = Enabled,
            Endpoint = Endpoint!
        });

        _dbContext.AddEvent(new WebHookAddedEvent()
        {
            AddedByUserId = User.GetUserId()!.Value,
            CreatedUtc = _clock.UtcNow,
            Enabled = Enabled,
            Endpoint = Endpoint!,
            WebHookId = webHookId
        });

        await _dbContext.SaveChangesAsync();

        TempData.SetFlashSuccess("Web hook added");
        return RedirectToPage("WebHooks");
    }
}
