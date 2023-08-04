using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialName;

[VerifyQueryParameterSignature]
public class PreferredNameModel : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public PreferredNameModel(
        IdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext)
    {
        _linkGenerator = linkGenerator;
        _dbContext = dbContext;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    public bool HasMiddleName => !string.IsNullOrEmpty(MiddleName);

    public string? ExistingPreferredName { get; set; }

    [FromQuery(Name = "firstName")]
    public string? FirstName { get; set; }

    [FromQuery(Name = "middleName")]
    public string? MiddleName { get; set; }

    [FromQuery(Name = "lastName")]
    public string? LastName { get; set; }

    [FromQuery(Name = "fileName")]
    public string? FileName { get; set; }

    [FromQuery(Name = "fileId")]
    public string? FileId { get; set; }

    [BindProperty]
    [Display(Name = " ")]
    [Required(ErrorMessage = "Select which name to use")]
    public PreferredNameOption? PreferredNameChoice { get; set; }

    [BindProperty(SupportsGet = true)]
    [Display(Name = "Your preferred name")]
    [RequiredIfTrue(nameof(PreferredNameChoice), "PreferredName", ErrorMessage = "Enter your preferred name")]
    [StringLengthIfTrue(nameof(PreferredNameChoice), 200, "PreferredName", ErrorMessage = "Preferred name must be 200 characters or less")]
    public string? PreferredName { get; set; }

    public async Task OnGet()
    {
        string? preferredName = null;
        var userId = User.GetUserId();
        var user = await _dbContext.Users.Where(u => u.UserId == userId).SingleAsync();
        ExistingPreferredName = user.PreferredName;

        if (PreferredName is null)
        {
            preferredName = ExistingPreferredName;
        }
        else
        {
            preferredName = PreferredName;
        }

        if (preferredName == ExistingPreferredName)
        {
            PreferredNameChoice = PreferredNameOption.ExistingPreferredName;
            PreferredName = null;
        }
        else if (!string.IsNullOrEmpty(MiddleName) && preferredName == ExistingName(includeMiddleName: true))
        {
            PreferredNameChoice = PreferredNameOption.ExistingFullName;
            PreferredName = null;
        }
        else if (preferredName == ExistingName(includeMiddleName: false))
        {
            PreferredNameChoice = PreferredNameOption.ExistingName;
            PreferredName = null;
        }
        else
        {
            PreferredNameChoice = PreferredNameOption.PreferredName;
            PreferredName = preferredName;
        }

        ModelState.Clear();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var userId = User.GetUserId();
        var user = await _dbContext.Users.Where(u => u.UserId == userId).SingleAsync();
        string? existingPreferredName = user.PreferredName;

        if (PreferredNameChoice == PreferredNameOption.PreferredName && PreferredName == existingPreferredName)
        {
            ModelState.AddModelError(nameof(PreferredName), "The preferred name entered matches your existing preferred name");
            return this.PageWithErrors();
        }

        var preferredName = PreferredNameChoice switch
        {
            PreferredNameOption.ExistingPreferredName => existingPreferredName,
            PreferredNameOption.ExistingFullName => ExistingName(includeMiddleName: true),
            PreferredNameOption.ExistingName => ExistingName(includeMiddleName: false),
            PreferredNameOption.PreferredName => PreferredName,
            _ => throw new ArgumentOutOfRangeException(nameof(PreferredNameChoice), PreferredNameChoice, "Invalid preferred name option chosen")
        };

        return Redirect(_linkGenerator.AccountOfficialNameConfirm(FirstName!, MiddleName, LastName!, FileName!, FileId!, preferredName!, ClientRedirectInfo));
    }

    public string ExistingName(bool includeMiddleName)
    {
        return !includeMiddleName || string.IsNullOrEmpty(MiddleName) ? $"{FirstName} {LastName}" : $"{FirstName} {MiddleName} {LastName}";
    }
}
