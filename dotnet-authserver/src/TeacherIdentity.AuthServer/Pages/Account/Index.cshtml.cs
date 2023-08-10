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
    private readonly bool _dqtSynchronizationEnabled;

    public IndexModel(
        TeacherIdentityServerDbContext dbContext,
        IConfiguration configuration,
        IDqtApiClient dqtApiClient,
        TeacherIdentityApplicationManager applicationManager)
    {
        _dbContext = dbContext;
        _dqtApiClient = dqtApiClient;
        _applicationManager = applicationManager;
        _dqtSynchronizationEnabled = configuration.GetValue("DqtSynchronizationEnabled", false);
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();
    public string? ClientDisplayName { get; set; }

    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? OfficialFirstName { get; set; }
    public string? OfficialMiddleName { get; set; }
    public string? OfficialLastName { get; set; }
    public string? PreferredName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? MobileNumber { get; set; }
    public string? Trn { get; set; }
    public DateOnly? DqtDateOfBirth { get; set; }
    public bool PendingDqtNameChange { get; set; }
    public bool PendingDqtDateOfBirthChange { get; set; }
    public bool DateOfBirthConflict { get; set; }
    public bool NameChangeEnabled { get; set; }

    public async Task OnGet()
    {
        var userId = User.GetUserId();

        var user = await _dbContext.Users
            .Where(u => u.UserId == userId)
            .Select(u => new
            {
                u.FirstName,
                u.MiddleName,
                u.LastName,
                u.PreferredName,
                u.DateOfBirth,
                u.EmailAddress,
                u.MobileNumber,
                u.Trn
            })
            .SingleAsync();

        FirstName = user.FirstName;
        MiddleName = user.MiddleName;
        LastName = user.LastName;
        PreferredName = user.PreferredName;
        DateOfBirth = user.DateOfBirth;
        Email = user.EmailAddress;
        MobileNumber = user.MobileNumber;
        Trn = user.Trn;

        if (Trn is not null)
        {
            var dqtUser = await _dqtApiClient.GetTeacherByTrn(Trn) ??
                throw new Exception($"User with TRN '{Trn}' cannot be found in DQT.");

            OfficialFirstName = dqtUser.FirstName;
            OfficialMiddleName = dqtUser.MiddleName;
            OfficialLastName = dqtUser.LastName;
            DqtDateOfBirth = dqtUser.DateOfBirth;
            PendingDqtNameChange = dqtUser.PendingNameChange;
            PendingDqtDateOfBirthChange = dqtUser.PendingDateOfBirthChange;

            if (!DqtDateOfBirth.Equals(DateOfBirth))
            {
                DateOfBirthConflict = true;
                HttpContext.Features.Get<WebRequestEventFeature>()?.Event.AddTag("DateOfBirthConflict");
            }

            if (!_dqtSynchronizationEnabled)
            {
                NameChangeEnabled = true;
            }
        }
        else if (_dqtSynchronizationEnabled)
        {
            NameChangeEnabled = true;
        }

        if (ClientRedirectInfo is not null)
        {
            var client = await _applicationManager.FindByClientIdAsync(ClientRedirectInfo.ClientId);
            ClientDisplayName = await _applicationManager.GetDisplayNameAsync(client!);
        }
    }
}
