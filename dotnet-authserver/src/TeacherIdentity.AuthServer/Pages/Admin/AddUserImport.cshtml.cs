using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.BackgroundJobs;
using TeacherIdentity.AuthServer.Services.UserImport;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
public class AddUserImportModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IUserImportStorageService _userImportCsvStorageService;
    private readonly IBackgroundJobScheduler _backgroundJobScheduler;

    public AddUserImportModel(
        TeacherIdentityServerDbContext dbContext,
        IUserImportStorageService userImportCsvStorageService,
        IBackgroundJobScheduler backgroundJobScheduler)
    {
        _dbContext = dbContext;
        _userImportCsvStorageService = userImportCsvStorageService;
        _backgroundJobScheduler = backgroundJobScheduler;
    }

    [Required(ErrorMessage = "Select a file")]
    [BindProperty]
    public IFormFile? Upload { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        // Validate that it is a CSV file (although should be blocked by accept=".csv" on modern browsers).
        if (!Upload!.FileName.EndsWith(".csv"))
        {
            ModelState.AddModelError(nameof(Upload), "The selected file must be a CSV");
            return this.PageWithErrors();
        }

        // Validate that file is not empty
        if (Upload.Length == 0)
        {
            ModelState.AddModelError(nameof(Upload), "The selected file contains no records");
            return this.PageWithErrors();
        }

        // Validate that we have the expected headers in the CSV file
        using var stream = Upload.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
        csv!.Read();
        csv.ReadHeader();
        try
        {
            csv.ValidateHeader<UserImportRow>();

            // Need to also check we don't have extra columns (ValidateHeader doesn't check this unfortunately)
            if (csv.HeaderRecord!.Length != 5)
            {
                ModelState.AddModelError(nameof(Upload), "The selected file contains invalid headers");
                return this.PageWithErrors();
            }
        }
        catch (HeaderValidationException)
        {
            ModelState.AddModelError(nameof(Upload), "The selected file contains invalid headers");
            return this.PageWithErrors();
        }

        // Now validate there is at least one row of actual data
        if (!csv.Read())
        {
            ModelState.AddModelError(nameof(Upload), "The selected file contains no records");
            return this.PageWithErrors();
        }

        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var originalFilename = Upload.FileName;
        using var uploadStream = Upload.OpenReadStream();
        await _userImportCsvStorageService.Upload(uploadStream, storedFilename);

        var userImportJob = new UserImportJob
        {
            UserImportJobId = userImportJobId,
            StoredFilename = storedFilename,
            OriginalFilename = originalFilename,
            UserImportJobStatus = UserImportJobStatus.New,
            Uploaded = DateTime.UtcNow
        };

        _dbContext.UserImportJobs.Add(userImportJob);
        await _dbContext.SaveChangesAsync();

        await _backgroundJobScheduler.Enqueue<IUserImportProcessor>(p => p.Process(userImportJobId));

        TempData.SetFlashSuccess($"CSV {Upload?.FileName} uploaded");
        return RedirectToPage("UserImports");
    }
}
