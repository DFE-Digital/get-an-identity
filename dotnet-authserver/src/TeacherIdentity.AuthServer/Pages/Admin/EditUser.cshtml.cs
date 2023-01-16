using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class EditUserModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;

    public EditUserModel(TeacherIdentityServerDbContext dbContext, IDqtApiClient dqtApiClient)
    {
        _dbContext = dbContext;
        _dqtApiClient = dqtApiClient;
    }

    public DateTime Created { get; set; }

    public string? EmailAddress { get; set; }

    public string? Name { get; set; }

    public string? RegistrationClientDisplayName { get; set; }

    public bool HaveDqtRecord { get; set; }

    public bool CanChangeDqtRecord { get; set; }

    public string? DqtName { get; set; }

    public DateOnly? DqtDateOfBirth { get; set; }

    public string? DqtNationalInsuranceNumber { get; set; }

    public string? Trn { get; set; }

    [FromRoute]
    public Guid UserId { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var user = await _dbContext.Users
            .Where(u => u.UserId == UserId)
            .Select(u => new
            {
                u.UserId,
                u.EmailAddress,
                u.FirstName,
                u.LastName,
                u.DateOfBirth,
                u.Trn,
                u.Created,
                u.UserType,
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
        Name = $"{user.FirstName} {user.LastName}";
        RegistrationClientDisplayName = user.RegisteredWithClientDisplayName;
        Trn = user.Trn;
        HaveDqtRecord = user.Trn is not null;
        CanChangeDqtRecord = !HaveDqtRecord;

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
