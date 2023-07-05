using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;

namespace TeacherIdentity.AuthServer.Pages.Account.PreferredName;

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

    public bool HasMiddleName => !string.IsNullOrEmpty(User.GetMiddleName());

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
        if (PreferredName is null)
        {
            var userId = User.GetUserId();
            var user = await _dbContext.Users.Where(u => u.UserId == userId).SingleAsync();
            preferredName = user.PreferredName;
        }
        else
        {
            preferredName = PreferredName;
        }

        if (string.IsNullOrEmpty(preferredName))
        {
            return;
        }

        var middleName = User.GetMiddleName();
        if (!string.IsNullOrEmpty(middleName) && preferredName == ExistingName(includeMiddleName: true))
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

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var preferredName = PreferredNameChoice switch
        {
            PreferredNameOption.ExistingFullName => ExistingName(includeMiddleName: true),
            PreferredNameOption.ExistingName => ExistingName(includeMiddleName: false),
            PreferredNameOption.PreferredName => PreferredName,
            _ => throw new ArgumentOutOfRangeException(nameof(PreferredNameChoice), PreferredNameChoice, "Invalid preferred name option chosen")
        };

        return Redirect(_linkGenerator.AccountPreferredNameConfirm(preferredName!, ClientRedirectInfo));
    }

    public string ExistingName(bool includeMiddleName)
    {
        var firstName = User.GetFirstName();
        var middleName = User.GetMiddleName();
        var lastName = User.GetLastName();

        return !includeMiddleName || string.IsNullOrEmpty(middleName) ? $"{firstName} {lastName}" : $"{firstName} {middleName} {lastName}";
    }
}
