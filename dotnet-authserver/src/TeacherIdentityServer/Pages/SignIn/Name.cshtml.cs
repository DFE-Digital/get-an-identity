using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentityServer.Models;

namespace TeacherIdentityServer.Pages.SignIn;

public class NameModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public NameModel(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [BindProperty]
    [Display(Name = "Your first name")]
    [Required(ErrorMessage = "Enter your first name")]
    public string? FirstName { get; set; }

    [BindProperty]
    [Display(Name = "Your last name")]
    [Required(ErrorMessage = "Enter your last name")]
    public string? LastName { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.FirstName = FirstName!;
        authenticationState.LastName = LastName!;

        var user = await RegisterUser();

        return await this.SignInUser(user);
    }

    private async Task<TeacherIdentityUser> RegisterUser()
    {
        var userId = Guid.NewGuid();
        var authenticationState = HttpContext.GetAuthenticationState();
        var email = authenticationState.EmailAddress;
        var firstName = authenticationState.FirstName;
        var lastName = authenticationState.LastName;

        var user = new TeacherIdentityUser()
        {
            UserId = userId,
            EmailAddress = email,
            FirstName = firstName,
            LastName = lastName
        };

        _dbContext.Add(user);

        await _dbContext.SaveChangesAsync();

        return user;
    }
}
