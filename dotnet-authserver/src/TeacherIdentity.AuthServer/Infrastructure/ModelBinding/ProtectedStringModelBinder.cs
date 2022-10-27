using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TeacherIdentity.AuthServer.Infrastructure.ModelBinding;

public class ProtectedStringModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.UnderlyingOrModelType == typeof(ProtectedString))
        {
            var protectedStringFactory = context.Services.GetRequiredService<ProtectedStringFactory>();
            return new ProtectedStringModelBinder(protectedStringFactory);
        }

        return null;
    }
}

public class ProtectedStringModelBinder : IModelBinder
{
    private readonly ProtectedStringFactory _protectedStringFactory;

    public ProtectedStringModelBinder(ProtectedStringFactory protectedStringFactory)
    {
        _protectedStringFactory = protectedStringFactory;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (string.IsNullOrEmpty(valueProviderResult.FirstValue))
        {
            return Task.CompletedTask;
        }

        if (_protectedStringFactory.TryCreateFromEncryptedValue(valueProviderResult.FirstValue, out var result))
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
