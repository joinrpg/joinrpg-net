using JoinRpg.DomainTypes.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace JoinRpg.Portal.Infrastructure.ModelBinding;

public sealed class ProjectEntityIdModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;
        if (modelType == typeof(ProjectIdentification) || !typeof(IProjectEntityId).IsAssignableFrom(modelType))
        {
            return null;
        }

        var allowAnotherProject = context.Metadata is DefaultModelMetadata defaultMetadata
            && defaultMetadata.Attributes.ParameterAttributes?.OfType<AllowAnotherProjectAttribute>().Any() == true;

        var binderType = typeof(ProjectEntityIdModelBinder<>).MakeGenericType(modelType);
        return (IModelBinder)Activator.CreateInstance(binderType, [allowAnotherProject])!;
    }
}
