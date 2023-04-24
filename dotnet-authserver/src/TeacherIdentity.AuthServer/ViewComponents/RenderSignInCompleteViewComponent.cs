using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentity.AuthServer.ViewComponents;

[ViewComponent(Name = "SignInComplete")]
public class RenderSignInCompleteViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(AuthenticationJourneyType? journeyType)
    {
        if (journeyType is null)
        {
            throw new ArgumentNullException(nameof(journeyType));
        }

        return Task.FromResult<IViewComponentResult>(journeyType == AuthenticationJourneyType.LegacyTrn ?
            View("~/Pages/SignIn/_SignIn.Complete.LegacyTRN.Content.cshtml") :
            View("~/Pages/SignIn/_SignIn.Complete.Default.Content.cshtml"));
    }
}
