using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.TrnLookup;

namespace TeacherIdentity.AuthServer.Pages.StubFindALostTrn;

[BindProperties]
[IgnoreAntiforgeryToken]
public class IdentityModel : PageModel
{
    private readonly FindALostTrnIntegrationHelper _findALostTrnIntegrationHelper;

    public IdentityModel(FindALostTrnIntegrationHelper findALostTrnIntegrationHelper)
    {
        _findALostTrnIntegrationHelper = findALostTrnIntegrationHelper;
    }

    [FromForm(Name = "email")]
    public string? Email { get; set; }

    [FromForm(Name = "journey_id")]
    public Guid JourneyId { get; set; }

    [FromForm(Name = "previous_url")]
    public string? PreviousUrl { get; set; }

    [FromForm(Name = "redirect_url")]
    public string? RedirectUrl { get; set; }

    [FromForm(Name = "session_id")]
    public string? SessionId { get; set; }

    public IActionResult OnPost()
    {
        var handoverParameters = Request.Form.ToDictionary(k => k.Key, k => k.Value.ToString());
        var passedSig = handoverParameters["sig"];
        handoverParameters.Remove("sig");

        var sig = _findALostTrnIntegrationHelper.CalculateSignature(handoverParameters);

        if (!sig.Equals(passedSig, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        HttpContext.Session.SetString("FindALostTrn:Email", Email!);
        HttpContext.Session.SetString("FindALostTrn:JourneyId", JourneyId.ToString());
        HttpContext.Session.SetString("FindALostTrn:PreviousUrl", PreviousUrl!);
        HttpContext.Session.SetString("FindALostTrn:RedirectUrl", RedirectUrl!);

        return RedirectToPage("Index");
    }
}
