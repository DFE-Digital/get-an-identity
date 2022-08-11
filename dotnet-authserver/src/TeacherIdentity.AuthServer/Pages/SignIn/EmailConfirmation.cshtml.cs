using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class EmailConfirmationModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public EmailConfirmationModel(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string? Email => HttpContext.GetAuthenticationState().EmailAddress;

    [BindProperty]
    [Display(Name = "Enter your code")]
    [Required(ErrorMessage = "Enter your confirmation code")]
    public string? Code { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        // TODO Validate code

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var authenticationState = HttpContext.GetAuthenticationState();

        var user = await _dbContext.Users.Where(u => u.EmailAddress == Email).SingleOrDefaultAsync();
        if (user is not null)
        {
            await HttpContext.SignInUser(user);
            authenticationState.FirstTimeUser = false;
        }
        else
        {
            authenticationState.EmailAddressConfirmed = true;
            authenticationState.FirstTimeUser = true;
        }

        return Redirect(authenticationState.GetNextHopUrl(Url));
    }
}
