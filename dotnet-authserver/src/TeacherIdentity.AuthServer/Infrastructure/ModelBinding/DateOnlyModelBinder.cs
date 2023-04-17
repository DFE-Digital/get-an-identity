using GovUk.Frontend.AspNetCore;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace TeacherIdentity.AuthServer.Infrastructure.ModelBinding;

public class DateOnlyModelBinder : IModelBinder
{
    public const string Format = "yyyy-MM-dd";

    private readonly IModelBinder _fallbackBinder;

    public DateOnlyModelBinder(IModelBinder fallbackBinder)
    {
        _fallbackBinder = fallbackBinder;
    }

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
        else
        {
            return _fallbackBinder.BindModelAsync(bindingContext);
        }

        return Task.CompletedTask;
    }
}

public class DateOnlyModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.UnderlyingOrModelType == typeof(DateOnly))
        {
            var gfaOptions = context.Services.GetRequiredService<IOptions<GovUkFrontendAspNetCoreOptions>>().Value;
            var fallbackBinder = gfaOptions.GetDateInputModelBinder(typeof(DateOnly))!;
            return new DateOnlyModelBinder(fallbackBinder);
        }

        return null;
    }
}
