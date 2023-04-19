using Dfe.Analytics.AspNetCore;
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

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();
    public string? ClientDisplayName { get; set; }

    public string? Name { get; set; }
    public string? OfficialName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? MobileNumber { get; set; }
    public string? Trn { get; set; }
    public DateOnly? DqtDateOfBirth { get; set; }
    public bool PendingDqtNameChange { get; set; }
    public bool PendingDqtDateOfBirthChange { get; set; }
    public bool DateOfBirthConflict { get; set; }

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

            OfficialName = string.Join(' ', new[] { dqtUser.FirstName, dqtUser.MiddleName, dqtUser.LastName }
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrEmpty(n)));

            DqtDateOfBirth = dqtUser.DateOfBirth;
            PendingDqtNameChange = dqtUser.PendingNameChange;
            PendingDqtDateOfBirthChange = dqtUser.PendingDateOfBirthChange;

            if (!DqtDateOfBirth.Equals(DateOfBirth))
            {
                DateOfBirthConflict = true;
                HttpContext.Features.Get<WebRequestEventFeature>()?.Event.AddTag("DateOfBirthConflict");
            }
        }

        if (ClientRedirectInfo is not null)
        {
            var client = await _applicationManager.FindByClientIdAsync(ClientRedirectInfo.ClientId);
            ClientDisplayName = await _applicationManager.GetDisplayNameAsync(client!);
        }
    }
}
