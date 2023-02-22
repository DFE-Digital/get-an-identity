using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
public class UserImportModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public UserImportModel(
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [FromRoute]
    public Guid UserImportJobId { get; set; }

    public string? Filename { get; set; }

    public string? Uploaded { get; set; }

    public string? Status { get; set; }

    public string? SuccessSummary { get; set; }

    public UserImportJobRowInfo[]? UserImportRows { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var userImportJob = await _dbContext.UserImportJobs.SingleOrDefaultAsync(j => j.UserImportJobId == UserImportJobId);
        if (userImportJob is null)
        {
            return NotFound();
        }

        Filename = userImportJob.OriginalFilename;
        Uploaded = userImportJob.Uploaded.ToString("dd/MM/yyyy HH:mm");
        Status = userImportJob.UserImportJobStatus.ToString();

        List<UserImportJobRow> userImportJobRows = await _dbContext.UserImportJobRows.Where(r => r.UserImportJobId == UserImportJobId).ToListAsync();
        if (userImportJobRows.Count > 0)
        {
            SuccessSummary = $"{userImportJobRows.Count(r => r.UserId != null)} / {userImportJobRows.Count()}";
            UserImportRows = userImportJobRows.Select(r => new UserImportJobRowInfo
            {
                RowNumber = r.RowNumber,
                Id = r.Id ?? "{null}",
                UserId = r.UserId is null ? "{null}" : r.UserId.Value.ToString(),
                ErrorCount = r.Errors is null ? 0 : r.Errors.Count,
                Errors = r.Errors ?? new List<string>()
            }).ToArray();
        }
        else
        {
            SuccessSummary = "TBC";
            UserImportRows = new UserImportJobRowInfo[] { };
        }

        return Page();
    }

    public async Task<IActionResult> OnGetDownloadFile()
    {
        using var stream = new MemoryStream();

        List<UserImportJobRow> userImportJobRows = await _dbContext.UserImportJobRows.Where(r => r.UserImportJobId == UserImportJobId).ToListAsync();
        if (userImportJobRows.Count > 0)
        {
            var data = userImportJobRows.Select(
                r => new
                {
                    RowNumber = r.RowNumber,
                    Id = r.Id,
                    UserId = r.UserId,
                    Errors = r.Errors is null ? null : string.Join(".", r.Errors),
                    RawData = r.RawData
                });

            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
            await csv.WriteRecordsAsync(data);
            writer.Flush();
        }

        return File(stream.ToArray(), "application/octet-stream", $"userimportdetails{_clock.UtcNow:yyyyMMddHHmmss}.csv");
    }

    public class UserImportJobRowInfo
    {
        public required int RowNumber { get; init; }
        public required string Id { get; init; }
        public required string UserId { get; init; }
        public required int ErrorCount { get; init; }
        public required List<string> Errors { get; init; }
    }

}
