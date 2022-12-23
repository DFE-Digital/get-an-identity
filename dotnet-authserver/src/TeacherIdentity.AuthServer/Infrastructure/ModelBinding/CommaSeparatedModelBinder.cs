using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TeacherIdentity.AuthServer.Infrastructure.ModelBinding;

public class CommaSeparatedModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var modelType = bindingContext.ModelType;
        if (!modelType.IsArray || modelType.GetArrayRank() != 1)
        {
            throw new NotSupportedException("This model binder only supports array types.");
        }

        var underlyingType = modelType.GetElementType()!;
        var typeConverter = TypeDescriptor.GetConverter(underlyingType);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        var splitValues = valueProviderResult.FirstValue!.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var result = Array.CreateInstance(underlyingType, splitValues.Length);

        for (var i = 0; i < splitValues.Length; i++)
        {
            var converted = typeConverter.ConvertFrom(splitValues[i]);
            result.SetValue(converted!, i);
        }

        bindingContext.Result = ModelBindingResult.Success(result);

        return Task.CompletedTask;
    }
}
