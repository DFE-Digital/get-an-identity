using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

public class WebHooksModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public WebHooksModel(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public WebHookInfo[]? WebHooks { get; set; }

    public async Task OnGet()
    {
        WebHooks = await _dbContext.WebHooks
            .OrderBy(e => e.Endpoint)
            .Select(e => new WebHookInfo()
            {
                Endpoint = e.Endpoint,
                Enabled = e.Enabled,
                WebHookId = e.WebHookId
            })
            .ToArrayAsync();
    }

    public class WebHookInfo
    {
        public required Guid WebHookId { get; init; }
        public required string Endpoint { get; init; }
        public required bool Enabled { get; init; }
    }
}
