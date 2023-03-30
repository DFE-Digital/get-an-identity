using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TeacherIdentity.AuthServer.Infrastructure.ModelBinding;

public class DateOnlyModelBinder : IModelBinder
{
    public const string Format = "yyyy-MM-dd";

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (!string.IsNullOrEmpty(value.FirstValue))
        {
            if (DateOnly.TryParseExact(value.FirstValue, Format, out var result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
        }

        return Task.CompletedTask;
    }
}

public class DateOnlyModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.UnderlyingOrModelType == typeof(DateOnly) &&
            (context.BindingInfo.BindingSource == BindingSource.Query ||
            (context.BindingInfo.BindingSource == BindingSource.Path)))
        {
            return new DateOnlyModelBinder();
        }

        return null;
    }
}
