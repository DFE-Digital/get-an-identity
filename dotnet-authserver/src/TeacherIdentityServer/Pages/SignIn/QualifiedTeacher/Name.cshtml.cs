using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentityServer.Models;

namespace TeacherIdentityServer.Pages.SignIn.QualifiedTeacher;

public enum HaveYouEverChangedYourNameOption
{
    [Display(Name = "No, I haven’t changed my name")]
    No,

    [Display(Name = "Yes, I changed my name")]
    Yes,

    [Display(Name = "Prefer not to say")]
    PreferNotToSay
}

[BindProperties]
public class NameModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public NameModel(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Display(Name = "Your first name")]
    [Required(ErrorMessage = "xxx")]
    public string? FirstName { get; set; }

    [Display(Name = "Your last name")]
    [Required(ErrorMessage = "xxx")]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "xxx")]
    public HaveYouEverChangedYourNameOption? HaveYouEverChangedYourName { get; set; }

    [Display(Name = "Previous first name (optional)")]
    public string? PreviousFirstName { get; set; }

    [Display(Name = "Previous last name (optional)")]
    public string? PreviousLastName { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.FirstName = FirstName!;
        authenticationState.LastName = LastName!;
        authenticationState.PreviousFirstName = PreviousFirstName;
        authenticationState.PreviousLastName = PreviousLastName;

        return Redirect(Url.QualifiedTeacherDateOfBirth());
    }

    //private async Task<TeacherIdentityUser> RegisterUser()
    //{
    //    var userId = Guid.NewGuid();
    //    var authModel = HttpContext.Session.GetAuthenticateModel();
    //    var email = authModel.EmailAddress;
    //    var firstName = authModel.FirstName;
    //    var lastName = authModel.LastName;

    //    var user = new TeacherIdentityUser()
    //    {
    //        UserId = userId,
    //        EmailAddress = email,
    //        FirstName = firstName,
    //        LastName = lastName
    //    };

    //    _dbContext.Add(user);

    //    await _dbContext.SaveChangesAsync();

    //    return user;
    //}
}
