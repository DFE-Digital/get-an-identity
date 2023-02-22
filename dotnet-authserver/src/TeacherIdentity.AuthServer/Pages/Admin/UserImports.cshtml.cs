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
        var jobs = await _dbContext.UserImportJobs
            .Select(j => new
            {
                Job = j,
                ImportedCount = _dbContext.UserImportJobRows.Where(r => r.UserImportJobId == j.UserImportJobId).Count(r => r.UserId != null),
                TotalRows = _dbContext.UserImportJobRows.Where(r => r.UserImportJobId == j.UserImportJobId).Count()
            })
            .ToArrayAsync();

        UserImports = jobs
            .OrderByDescending(j => j.Job.Uploaded)
            .Select(j => new UserImportJobInfo()
            {
                UserImportJobId = j.Job.UserImportJobId,
                Filename = j.Job.OriginalFilename,
                Uploaded = j.Job.Uploaded.ToString("dd/MM/yyyy HH:mm"),
                Status = j.Job.UserImportJobStatus.ToString(),
                SuccessSummary = $"{j.ImportedCount} / {j.TotalRows}"
            })
            .ToArray();
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
