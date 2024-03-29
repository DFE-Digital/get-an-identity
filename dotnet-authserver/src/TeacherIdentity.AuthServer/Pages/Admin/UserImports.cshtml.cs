using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
                AddedCount = _dbContext.UserImportJobRows.Where(r => r.UserImportJobId == j.UserImportJobId).Count(r => r.UserImportRowResult == UserImportRowResult.UserAdded),
                UpdatedCount = _dbContext.UserImportJobRows.Where(r => r.UserImportJobId == j.UserImportJobId).Count(r => r.UserImportRowResult == UserImportRowResult.UserUpdated),
                InvalidCount = _dbContext.UserImportJobRows.Where(r => r.UserImportJobId == j.UserImportJobId).Count(r => r.UserImportRowResult == UserImportRowResult.Invalid),
                NoActionCount = _dbContext.UserImportJobRows.Where(r => r.UserImportJobId == j.UserImportJobId).Count(r => r.UserImportRowResult == UserImportRowResult.None),
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
                AddedCount = j.AddedCount,
                UpdatedCount = j.UpdatedCount,
                InvalidCount = j.InvalidCount,
                NoActionCount = j.NoActionCount,
                TotalRowsCount = j.TotalRows
            })
            .ToArray();
    }

    public class UserImportJobInfo
    {
        public required Guid UserImportJobId { get; init; }
        public required string Filename { get; init; }
        public required string Uploaded { get; init; }
        public required string Status { get; init; }
        public required int AddedCount { get; init; }
        public required int UpdatedCount { get; init; }
        public required int InvalidCount { get; init; }
        public required int NoActionCount { get; init; }
        public required int TotalRowsCount { get; init; }
    }
}
