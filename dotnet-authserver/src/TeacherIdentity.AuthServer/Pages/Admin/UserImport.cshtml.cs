using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
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

    public int AddedCount { get; set; }

    public int UpdatedCount { get; set; }
    public int InvalidCount { get; set; }
    public int NoActionCount { get; set; }
    public int TotalRowsCount { get; set; }

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
            AddedCount = userImportJobRows.Count(r => r.UserImportRowResult == UserImportRowResult.UserAdded);
            UpdatedCount = userImportJobRows.Count(r => r.UserImportRowResult == UserImportRowResult.UserUpdated);
            InvalidCount = userImportJobRows.Count(r => r.UserImportRowResult == UserImportRowResult.Invalid);
            NoActionCount = userImportJobRows.Count(r => r.UserImportRowResult == UserImportRowResult.None);
            TotalRowsCount = userImportJobRows.Count();
            UserImportRows = userImportJobRows.Select(r => new UserImportJobRowInfo
            {
                RowNumber = r.RowNumber,
                Id = r.Id ?? "{null}",
                UserId = r.UserId is null ? "{null}" : r.UserId.Value.ToString(),
                NotesCount = r.Notes is null ? 0 : r.Notes.Count,
                Notes = r.Notes ?? new List<string>(),
                UserImportRowResult = r.UserImportRowResult
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
                r => new UserImportDownloadRow
                {
                    RowNumber = r.RowNumber,
                    Id = r.Id,
                    UserId = r.UserId,
                    UserImportRowResult = r.UserImportRowResult,
                    Notes = r.Notes is null ? null : string.Join(". ", r.Notes),
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
        public required int NotesCount { get; init; }
        public required List<string> Notes { get; init; }
        public required UserImportRowResult UserImportRowResult { get; set; }
    }

    public class UserImportDownloadRow
    {
        [Name("ROW_NUMBER")]
        public required int RowNumber { get; init; }
        [Name("ID")]
        public string? Id { get; init; }
        [Name("USER_ID")]
        public Guid? UserId { get; init; }
        [Name("USER_IMPORT_ROW_RESULT")]
        public required UserImportRowResult UserImportRowResult { get; set; }
        [Name("NOTES")]
        public string? Notes { get; init; }
        [Name("RAW_DATA")]
        public string? RawData { get; init; }
    }

}
