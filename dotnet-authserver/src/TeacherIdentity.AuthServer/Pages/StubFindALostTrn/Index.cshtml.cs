using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Infrastructure.Security;

namespace TeacherIdentity.AuthServer.Pages.StubFindALostTrn;

public class IndexModel : PageModel
{
    private readonly IApiClientRepository _apiClientRepository;

    public IndexModel(IApiClientRepository apiClientRepository)
    {
        _apiClientRepository = apiClientRepository;
    }

    [BindProperty]
    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter your email address")]
    public string? Email { get; set; }
    [BindProperty]
    [Display(Name = "Official first name")]
    [Required(ErrorMessage = "Enter your official first name")]
    public string? OfficialFirstName { get; set; }

    [BindProperty]
    [Display(Name = "Official Last name")]
    [Required(ErrorMessage = "Enter your official last name")]
    public string? OfficialLastName { get; set; }

    [BindProperty]
    [Display(Name = "Preferred first name")]
    public string? PreferredFirstName { get; set; }

    [BindProperty]
    [Display(Name = "Preferred Last name")]
    public string? PreferredLastName { get; set; }

    [BindProperty]
    [Display(Name = "Date of birth")]
    [Required(ErrorMessage = "Enter your date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [BindProperty]
    [Display(Name = "TRN")]
    public string? Trn { get; set; }

    public string? PreviousUrl { get; private set; }

    public void OnGet()
    {
        Email = HttpContext.Session.GetString("FindALostTrn:Email");
        PreviousUrl = HttpContext.Session.GetString("FindALostTrn:PreviousUrl");
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await PersistLookupState();

        var redirectUrl = HttpContext.Session.GetString("FindALostTrn:RedirectUrl")!;
        return Redirect(redirectUrl);

        async Task PersistLookupState()
        {
            var apiKey = _apiClientRepository.GetClientByClientId("stub-find")?.ApiKeys?.First() ??
                throw new InvalidOperationException("No API key found for stub-find.");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            httpClient.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");

            var journeyId = HttpContext.Session.GetString("FindALostTrn:JourneyId");

            var response = await httpClient.PutAsJsonAsync(
                $"/api/find-trn/user/{journeyId}",
                new
                {
                    OfficialFirstName = OfficialFirstName,
                    OfficialLastName = OfficialLastName,
                    DateOfBirth = DateOfBirth!.Value.ToString("yyyy-MM-dd"),
                    Trn = Trn,
                    PreferredFirstName = PreferredFirstName!,
                    PreferredLastName = PreferredLastName!,
                });
            response.EnsureSuccessStatusCode();
        }
    }
}
