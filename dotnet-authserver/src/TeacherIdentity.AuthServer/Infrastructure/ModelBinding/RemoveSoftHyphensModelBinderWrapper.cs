using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TeacherIdentity.AuthServer.Infrastructure.ModelBinding;

public class RemoveSoftHyphensModelBinderWrapper : IModelBinder
{
    private readonly IModelBinder _innerBinder;

    public RemoveSoftHyphensModelBinderWrapper(IModelBinder innerBinder)
    {
        _innerBinder = innerBinder;
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        await _innerBinder.BindModelAsync(bindingContext);

        if (bindingContext.Result.IsModelSet)
        {
            var value = (string?)bindingContext.Result.Model;
            var withoutSoftHyphens = value?.Replace("\u00AD", "");
            bindingContext.Result = ModelBindingResult.Success(withoutSoftHyphens);
        }
    }
}
