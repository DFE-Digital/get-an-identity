using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentityServer.Models;

namespace TeacherIdentityServer.Pages;

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

    [FromQuery]
    public string ReturnUrl { get; set; } = null!;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        HttpContext.Session.UpdateAuthenticateModel(authModel =>
        {
            authModel.FirstName = FirstName!;
            authModel.LastName = LastName!;
        });

        var user = await RegisterUser();

        return await this.SignInUser(user, ReturnUrl);
    }

    private async Task<TeacherIdentityUser> RegisterUser()
    {
        var userId = Guid.NewGuid();
        var authModel = HttpContext.Session.GetAuthenticateModel();
        var email = authModel.EmailAddress;
        var firstName = authModel.FirstName;
        var lastName = authModel.LastName;

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
