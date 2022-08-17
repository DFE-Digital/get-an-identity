using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.StubFindALostTrn;

public class IndexModel : PageModel
{
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
    public string? Trn { get; set; }

    public string? RedirectUri { get; set; }

    public void OnGet()
    {
        Email = HttpContext.Session.GetString("FindALostTrn:Email");
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await PersistLookupState();

        var redirectUri = HttpContext.Session.GetString("FindALostTrn:RedirectUri")!;
        return Redirect(redirectUri);

        async Task PersistLookupState()
        {
            var apiKey = "stub-find";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            httpClient.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");

            var journeyId = HttpContext.Session.GetString("FindALostTrn:JourneyId");

            await httpClient.PutAsJsonAsync($"/api/find-trn/user/{journeyId}", new
            {
                FirstName = FirstName!,
                LastName = LastName!,
                DateOfBirth = DateOfBirth!.Value.ToString("yyyy-MM-dd"),
                Trn = Trn
            });
        }
    }
}
