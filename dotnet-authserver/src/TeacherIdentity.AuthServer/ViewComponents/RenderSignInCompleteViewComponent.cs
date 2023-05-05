using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.ViewComponents;

[ViewComponent(Name = "SignInComplete")]
public class RenderSignInCompleteViewComponent : ViewComponent
{
    private readonly IViewComponentHelper _viewComponentHelper;

    public RenderSignInCompleteViewComponent(IViewComponentHelper viewComponentHelper)
    {
        _viewComponentHelper = viewComponentHelper;
    }

    public IViewComponentResult Invoke(TrnRequirementType? trnRequirementType, TrnLookupStatus? trnLookupStatus)
    {
        if (trnRequirementType is null)
        {
            return View($"~/Pages/SignIn/_SignIn.Complete.Core.NoTrnLookup.cshtml");
        }

        if (trnRequirementType == TrnRequirementType.Legacy)
        {
            return View("~/Pages/SignIn/_SignIn.Complete.LegacyTRN.Content.cshtml");
        }

        var trnState = trnLookupStatus switch
        {
            TrnLookupStatus.None => "TrnNotFound",
            TrnLookupStatus.Failed => "TrnNotFound",
            TrnLookupStatus.Found => "TrnFound",
            TrnLookupStatus.Pending => "TrnPending",
            null when trnRequirementType == TrnRequirementType.Optional => "NoTrnLookup",
            _ => throw new ArgumentOutOfRangeException(nameof(trnLookupStatus), trnLookupStatus, "Invalid TRN lookup status")
        };

        return View($"~/Pages/SignIn/_SignIn.Complete.Core.Trn{trnRequirementType}.{trnState}.cshtml");
    }
}
