using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Flurl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeacherIdentityServer;

namespace StubFindALostTrnServer.MyFeature.Pages;

[BindProperties]
public class IndexModel : PageModel
{
    private readonly FindALostTrnIntegrationOptions _options;

    public IndexModel(IOptions<FindALostTrnIntegrationOptions> options)
    {
        _options = options.Value;
    }

    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter your email address")]
    public string? Email { get; set; }

    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter your first name")]
    public string? FirstName { get; set; }

    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter your last name")]
    public string? LastName { get; set; }

    [Display(Name = "Date of birth")]
    [Required(ErrorMessage = "Enter your date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "TRN")]
    [Required(ErrorMessage = "Enter TRN")]
    public string? Trn { get; set; }

    [FromQuery(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    public void OnGet()
    {
        Email = Request.Query["email"];
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var pskBytes = Convert.FromBase64String(_options.SharedKey);
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
            Expires = DateTime.UtcNow.AddDays(1),
            //Issuer = "https://find-a-lost-trn.education.gov.uk/",
            //Audience = "https://authserveruri/",
            SigningCredentials = signingCredentials
        });

        var callbackUrl = new Url(RedirectUri).SetQueryParam("user", jwt);

        return Redirect(callbackUrl);
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        // TODO Validate signature
    }
}
