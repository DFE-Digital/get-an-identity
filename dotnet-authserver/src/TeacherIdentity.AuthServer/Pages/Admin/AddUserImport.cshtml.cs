using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.Csv;

namespace TeacherIdentity.AuthServer.Pages.Admin;

public class AddUserImportModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IUserImportCsvStorageService _userImportCsvStorageService;

    public AddUserImportModel(
        TeacherIdentityServerDbContext dbContext,
        IUserImportCsvStorageService userImportCsvStorageService)
    {
        _dbContext = dbContext;
        _userImportCsvStorageService = userImportCsvStorageService;
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

        if (Upload == null)
        {
            ModelState.AddModelError(nameof(Upload), "Select a file");
            return this.PageWithErrors();
        }

        // Validate that it is a CSV file (although should be blocked by accept=".csv" on modern browsers).
        if (!Upload.FileName.EndsWith(".csv"))
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
        csv.Read();
        csv.ReadHeader();
        var hasInvalidHeaders = false;
        try
        {
            csv.ValidateHeader<UserImportRow>();

            // Need to also check we don't have extra columns (ValidateHeader doesn't check this unfortunately)
            if (csv?.HeaderRecord?.Length != 5)
            {
                hasInvalidHeaders = true;
            }
        }
        catch (HeaderValidationException)
        {
            hasInvalidHeaders = true;
        }

        if (hasInvalidHeaders)
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

        TempData.SetFlashSuccess($"CSV {Upload?.FileName} uploaded");
        return RedirectToPage("UserImports");
    }
}
