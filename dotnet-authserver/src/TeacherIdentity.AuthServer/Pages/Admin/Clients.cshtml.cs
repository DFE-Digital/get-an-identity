using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
public class ClientsModel : PageModel
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    public ClientsModel(IOpenIddictApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    public ClientInfo[]? Clients { get; set; }

    public async Task OnGet()
    {
        Clients = (await _applicationManager.ListAsync().ToListAsync())
            .Cast<Application>()
            .Select(a => new ClientInfo()
            {
                ClientId = a.ClientId!,
                DisplayName = a.DisplayName!
            })
            .OrderBy(a => a.ClientId)
            .ToArray();
    }

    public class ClientInfo
    {
        public string ClientId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }
}
