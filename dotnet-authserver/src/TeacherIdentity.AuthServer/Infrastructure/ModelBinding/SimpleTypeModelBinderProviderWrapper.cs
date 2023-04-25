using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace TeacherIdentity.AuthServer.Infrastructure.ModelBinding;

public class SimpleTypeModelBinderProviderWrapper : IModelBinderProvider
{
    private readonly SimpleTypeModelBinderProvider _innerProvider;

    public SimpleTypeModelBinderProviderWrapper(SimpleTypeModelBinderProvider innerProvider)
    {
        _innerProvider = innerProvider;
    }

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var innerBinder = _innerProvider.GetBinder(context);

        if (innerBinder is not null &&
            context.Metadata is DefaultModelMetadata defaultModelMetadata &&
            defaultModelMetadata.Attributes.Attributes.OfType<EmailAddressAttribute>().Any())
        {
            return new RemoveSoftHyphensModelBinderWrapper(innerBinder);
        }

        return innerBinder;
    }
}
