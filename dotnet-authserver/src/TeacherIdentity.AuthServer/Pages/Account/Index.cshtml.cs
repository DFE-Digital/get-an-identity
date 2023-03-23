using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Account;

public class IndexModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly TeacherIdentityApplicationManager _applicationManager;

    public IndexModel(
        TeacherIdentityServerDbContext dbContext,
        IDqtApiClient dqtApiClient,
        TeacherIdentityApplicationManager applicationManager)
    {
        _dbContext = dbContext;
        _dqtApiClient = dqtApiClient;
        _applicationManager = applicationManager;
    }

    [FromQuery(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromQuery(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    public string? ClientDisplayName { get; set; }
    public string? SafeRedirectUri { get; set; }

    public string? Name { get; set; }
    public string? OfficialName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? MobileNumber { get; set; }
    public string? Trn { get; set; }
    public DateOnly? DqtDateOfBirth { get; set; }
    public bool PendingDqtNameChange { get; set; }
    public bool PendingDqtDateOfBirthChange { get; set; }

    public async Task OnGet()
    {
        var userId = User.GetUserId()!.Value;

        var user = await _dbContext.Users
            .Where(u => u.UserId == userId)
            .Select(u => new
            {
                u.FirstName,
                u.LastName,
                u.DateOfBirth,
                u.EmailAddress,
                u.MobileNumber,
                u.Trn
            })
            .SingleAsync();

        Name = $"{user.FirstName} {user.LastName}";
        DateOfBirth = user.DateOfBirth;
        Email = user.EmailAddress;
        MobileNumber = user.MobileNumber;
        Trn = user.Trn;

        if (Trn is not null)
        {
            var dqtUser = await _dqtApiClient.GetTeacherByTrn(Trn) ??
                throw new Exception($"User with TRN '{Trn}' cannot be found in DQT.");

            OfficialName = $"{dqtUser.FirstName} {dqtUser.LastName}";
            DqtDateOfBirth = dqtUser.DateOfBirth;
            PendingDqtNameChange = dqtUser.PendingNameChange;
            PendingDqtDateOfBirthChange = dqtUser.PendingDateOfBirthChange;
        }
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        // Services link to this page from the 'Account' option in their nav.
        // They pass a `redirect_uri` query parameter along with their `client_id` so we know where to send the user
        // back to when they're done.
        // Check that the `redirect_uri` provided is valid for the specified client.
        //
        // Alternatively, if there's no `client_id` but there is a `redirect_uri` then require that it's a local URL.

        if (ClientId is not null && RedirectUri != null)
        {
            var client = await _applicationManager.FindByClientIdAsync(ClientId);

            if (client is null || !await _applicationManager.ValidateRedirectUriDomain(client, RedirectUri))
            {
                context.Result = OnInvalidParameters();
            }
            else
            {
                SafeRedirectUri = RedirectUri;
                ClientDisplayName = client.DisplayName;
            }
        }
        else if (!string.IsNullOrEmpty(RedirectUri))
        {
            if (Url.IsLocalUrl(RedirectUri))
            {
                SafeRedirectUri = RedirectUri;
            }
            else
            {
                context.Result = OnInvalidParameters();
            }
        }

        await base.OnPageHandlerExecutionAsync(context, next);

        IActionResult OnInvalidParameters() => BadRequest();
    }
}
