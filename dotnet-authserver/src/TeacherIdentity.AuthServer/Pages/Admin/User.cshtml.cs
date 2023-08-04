using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class UserModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;

    public UserModel(TeacherIdentityServerDbContext dbContext, IDqtApiClient dqtApiClient)
    {
        _dbContext = dbContext;
        _dqtApiClient = dqtApiClient;
    }

    public DateTime Created { get; set; }

    public string? EmailAddress { get; set; }

    public string? MobileNumber { get; set; }

    public string? Name { get; set; }

    public string? PreferredName { get; set; }

    public string? RegistrationClientDisplayName { get; set; }

    public bool HaveDqtRecord { get; set; }

    public bool CanChangeDqtRecord { get; set; }

    public string? DqtName { get; set; }

    public DateOnly? DqtDateOfBirth { get; set; }

    public string? DqtNationalInsuranceNumber { get; set; }

    public string? Trn { get; set; }

    public IEnumerable<Guid>? MergedUserIds { get; set; }

    [FromRoute]
    public Guid UserId { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.MergedUsers)
            .Where(u => u.UserId == UserId)
            .Select(u => new
            {
                u.UserId,
                u.EmailAddress,
                u.MobileNumber,
                u.FirstName,
                u.MiddleName,
                u.LastName,
                u.PreferredName,
                u.DateOfBirth,
                u.Trn,
                u.Created,
                u.UserType,
                MergedUserIds = u.MergedUsers!.Select(mu => mu.UserId),
                RegisteredWithClientDisplayName = u.RegisteredWithClient != null ? u.RegisteredWithClient.DisplayName : null
            })
            .SingleOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        if (user.UserType == UserType.Staff)
        {
            return RedirectToPage("EditStaffUser", new { UserId });
        }

        Created = user.Created;
        EmailAddress = user.EmailAddress;
        MobileNumber = user.MobileNumber;
        Name = string.IsNullOrWhiteSpace(user.MiddleName) ? $"{user.FirstName} {user.LastName}" : $"{user.FirstName} {user.MiddleName} {user.LastName}";
        PreferredName = user.PreferredName;
        RegistrationClientDisplayName = user.RegisteredWithClientDisplayName;
        Trn = user.Trn;
        HaveDqtRecord = user.Trn is not null;
        CanChangeDqtRecord = !HaveDqtRecord;
        MergedUserIds = user.MergedUserIds;

        if (user.Trn is not null)
        {
            var dqtUser = await _dqtApiClient.GetTeacherByTrn(user.Trn);

            if (dqtUser is null)
            {
                throw new InvalidOperationException($"Could not find DQT user with TRN: '{user.Trn}'.");
            }

            DqtName = $"{dqtUser.FirstName} {dqtUser.LastName}";
            DqtDateOfBirth = dqtUser.DateOfBirth;
            DqtNationalInsuranceNumber = dqtUser.NationalInsuranceNumber;
        }

        return Page();
    }
}
