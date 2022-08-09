using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;

namespace TeacherIdentity.AuthServer.Pages.StubFindALostTrn;

public class IndexModel : PageModel
{
    private readonly FindALostTrnIntegrationHelper _findALostTrnIntegrationHelper;

    public IndexModel(FindALostTrnIntegrationHelper findALostTrnIntegrationHelper)
    {
        _findALostTrnIntegrationHelper = findALostTrnIntegrationHelper;
    }

    [BindProperty]
    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter your email address")]
    public string? Email { get; set; }

    [BindProperty]
    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter your first name")]
    public string? FirstName { get; set; }

    [BindProperty]
    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter your last name")]
    public string? LastName { get; set; }

    [BindProperty]
    [Display(Name = "Date of birth")]
    [Required(ErrorMessage = "Enter your date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [BindProperty]
    [Display(Name = "TRN")]
    [Required(ErrorMessage = "Enter TRN")]
    public string? Trn { get; set; }

    public bool IsCallback { get; set; }

    public string? RedirectUri { get; set; }

    public string? UserJwt { get; set; }

    public void OnGet()
    {
        Email = HttpContext.Session.GetString("FindALostTrn:Email");
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var pskBytes = Encoding.UTF8.GetBytes(_findALostTrnIntegrationHelper.Options.SharedKey);
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(pskBytes), SecurityAlgorithms.HmacSha256Signature);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.CreateEncodedJwt(new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("email", Email!),
                new Claim("birthdate", DateOfBirth!.Value.ToString("yyyy-MM-dd")),
                new Claim("given_name", FirstName!),
                new Claim("family_name", LastName!),
                new Claim("trn", Trn!),
            }),
            SigningCredentials = signingCredentials
        });

        var redirectUri = HttpContext.Session.GetString("FindALostTrn:RedirectUri")!;

        // Yuk; we're re-using the same view here as we can't easily switch to another with Razor Pages
        IsCallback = true;
        RedirectUri = redirectUri;
        UserJwt = jwt;
        return Page();
    }
}
