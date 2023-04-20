using Microsoft.AspNetCore.Mvc.ModelBinding;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Infrastructure.ModelBinding;

public class WebHookMessageTypesModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var modelType = bindingContext.ModelType;
        if (!typeof(WebHookMessageTypes).IsAssignableTo(modelType))
        {
            throw new InvalidOperationException($"Cannot bind to {modelType}.");
        }

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        var rawValues = valueProviderResult.Values.ToArray();
        if (Enum.TryParse(string.Join(",", rawValues), out WebHookMessageTypes result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        else
        {
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }
}
