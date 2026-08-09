using System.Globalization;
using System.Reflection;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JoinRpg.Portal.Infrastructure.ModelBinding;

public sealed class ProjectEntityIdModelBinder<T>(bool allowAnotherProject) : IModelBinder
    where T : class, IProjectEntityId, ISpanParsable<T>
{
    // Голое число можно смёржить с текущим ProjectId только когда у T ровно один
    // int-компонент кроме ProjectId — то есть основной конструктор имеет вид (ProjectIdentification, int).
    // Для вложенных составных id (например PlotElementIdentification(PlotFolderIdentification, int))
    // такого конструктора нет, и число однозначно смёржить не с чем.
    private static readonly ConstructorInfo? LeafConstructor =
        typeof(T).GetConstructor([typeof(ProjectIdentification), typeof(int)]);

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;
        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        // Текущий проект может отсутствовать (экшн вне {ProjectId}-маршрута) — в этом случае
        // работает только полная строка-идентификатор, без проверки/подстановки проекта.
        ProjectIdentification? currentProjectId =
            bindingContext.HttpContext.Items.TryGetValue(Constants.ProjectIdName, out var raw) && raw is int rawProjectId
                ? new ProjectIdentification(rawProjectId)
                : null;

        if (int.TryParse(value, NumberStyles.Integer, valueProviderResult.Culture, out var leafId))
        {
            if (allowAnotherProject)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                    $"[AllowAnotherProject] требует полного идентификатора, а не просто числа: '{value}'");
                return Task.CompletedTask;
            }

            if (currentProjectId is null)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                    $"Не удалось определить текущий проект, укажите полный идентификатор {typeof(T).Name}: '{value}'");
                return Task.CompletedTask;
            }

            if (LeafConstructor is null)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                    $"{typeof(T).Name} нельзя однозначно определить по одному числу, укажите полный идентификатор: '{value}'");
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(
                (T)LeafConstructor.Invoke([currentProjectId, leafId]));
            return Task.CompletedTask;
        }

        if (!T.TryParse(value, valueProviderResult.Culture, out var parsed))
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                $"Не удалось распознать идентификатор {typeof(T).Name}: '{value}'");
            return Task.CompletedTask;
        }

        if (currentProjectId is not null && parsed.ProjectId != currentProjectId && !allowAnotherProject)
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                $"Идентификатор {typeof(T).Name} относится к другому проекту");
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(parsed);
        return Task.CompletedTask;
    }
}
