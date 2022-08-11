using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class ConfirmationModel : PageModel
{
    private readonly ILogger<ConfirmationModel> _logger;

    public ConfirmationModel(ILogger<ConfirmationModel> logger)
    {
        _logger = logger;
    }

    public string? Email { get; set; }

    public bool GotTrn { get; set; }

    public bool FirstTimeUser { get; set; }

    public string? Name { get; set; }

    public string? Trn { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public void OnGet()
    {
        var authenticationState = HttpContext.GetAuthenticationState();
        Email = authenticationState.EmailAddress;
        GotTrn = authenticationState.Trn is not null;
        FirstTimeUser = authenticationState.FirstTimeUser!.Value;
        Name = $"{authenticationState.FirstName} {authenticationState.LastName}";
        Trn = authenticationState.Trn;
        DateOfBirth = authenticationState.DateOfBirth!.Value;
    }

    public IActionResult OnPost()
    {
        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.HaveCompletedConfirmationPage = true;

        return Redirect(authenticationState.GetNextHopUrl(Url));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        if (!authenticationState.UserId.HasValue)
        {
            _logger.LogWarning($"Hit {nameof(ConfirmationModel)} without a known user.");
            context.Result = BadRequest();
        }
    }
}
