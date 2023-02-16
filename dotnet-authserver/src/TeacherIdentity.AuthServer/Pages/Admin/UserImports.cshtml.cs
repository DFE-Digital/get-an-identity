using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
public class UserImportsModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public UserImportsModel(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public UserImportJobInfo[]? UserImports { get; set; }

    public async Task OnGet()
    {
        UserImports = await _dbContext.UserImportJobs
            .OrderByDescending(j => j.Uploaded)
            .Select(j => new UserImportJobInfo()
            {
                UserImportJobId = j.UserImportJobId,
                Filename = j.OriginalFilename,
                Uploaded = j.Uploaded.ToString("dd/MM/yyyy HH:mm"),
                Status = j.UserImportJobStatus.ToString(),
                SuccessSummary = j.UserImportJobRows == null ? "TBC" : $"{j.UserImportJobRows.Count(r => r.UserId != null)} / {j.UserImportJobRows.Count()}"
            })
            .ToArrayAsync();
    }

    public class UserImportJobInfo
    {
        public required Guid UserImportJobId { get; init; }
        public required string Filename { get; init; }
        public required string Uploaded { get; init; }
        public required string Status { get; init; }
        public required string SuccessSummary { get; init; }
    }
}
