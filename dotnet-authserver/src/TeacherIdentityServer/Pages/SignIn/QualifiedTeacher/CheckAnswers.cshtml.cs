using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentityServer.Models;

namespace TeacherIdentityServer.Pages.SignIn.QualifiedTeacher;

public class CheckAnswersModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public CheckAnswersModel(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Display(Name = "Email address")]
    public string EmailAddress { get; set; } = null!;

    [Display(Name = "Name")]
    public string Name { get; set; } = null!;

    [Display(Name = "Date of birth")]
    public DateTime DateOfBirth { get; set; }

    [Display(Name = "National insurance number")]
    public string? Nino { get; set; }

    [Display(Name = "Teacher reference number (TRN)")]
    public string Trn { get; set; } = null!;

    [Display(Name = "Have you been awarded QTS?")]
    public bool HaveYouBeenAwardedQts { get; set; }

    [Display(Name = "Where did you get your QTS?")]
    public string? WhereDidYouGetYourQts { get; set; }

    public void OnGet()
    {
        var authenticationState = HttpContext.GetAuthenticationState();
        EmailAddress = authenticationState.EmailAddress!;
        Name = $"{authenticationState.FirstName} {authenticationState.LastName}";
        DateOfBirth = authenticationState.DateOfBirth!.Value;
        Nino = authenticationState.Nino;
        HaveYouBeenAwardedQts = authenticationState.HaveQts!.Value;
        WhereDidYouGetYourQts = authenticationState.QtsProviderName;
    }

    public async Task<IActionResult> OnPost()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        // We don't expect to have an existing user at this point
        if (authenticationState.UserId.HasValue)
        {
            throw new NotImplementedException();
        }

        var userId = Guid.NewGuid();
        var user = new TeacherIdentityUser()
        {
            EmailAddress = authenticationState.EmailAddress,
            FirstName = authenticationState.FirstName,
            LastName = authenticationState.LastName,
            Trn = authenticationState.Trn,
            UserId = userId
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        authenticationState.UserId = userId;

        return await HttpContext.SignInUser(user);
    }
}
