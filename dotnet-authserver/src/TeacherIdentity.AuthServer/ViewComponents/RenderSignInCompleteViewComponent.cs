using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
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

    public async Task<IViewComponentResult> InvokeAsync(
        TrnRequirementType? trnRequirementType,
        TrnLookupStatus? trnLookupStatus)
    {
        if (trnRequirementType is null)
        {
            throw new ArgumentNullException(nameof(trnRequirementType));
        }

        if (trnRequirementType == TrnRequirementType.Legacy)
        {
            return await Task.FromResult<IViewComponentResult>(View("~/Pages/SignIn/_SignIn.Complete.LegacyTRN.Content.cshtml"));
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

        return await Task.FromResult<IViewComponentResult>(View(
            $"~/Pages/SignIn/_SignIn.Complete.Core.Trn{trnRequirementType}.{trnState}.cshtml"));
    }
}
