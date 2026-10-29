using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Чистые валидаторы входных данных доменных операций.
/// </summary>
internal static class ServiceValidation
{
    public static string Required([NotNull] string? stringValue,
        [CallerArgumentExpression(nameof(stringValue))] string? fieldName = null)
    {
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            throw new FieldRequiredException(fieldName ?? "??unknown field");
        }

        return stringValue.Trim();
    }

    public static IReadOnlyCollection<T> Required<T>(IReadOnlyCollection<T> items)
    {
        if (items.Count == 0)
        {
            // Здесь это действительно «данные невалидны»: обязательный список пуст, и парная
            // перегрузка Required(string) точно так же сигналит об отсутствии обязательного значения.
            throw new JoinValidationException($"Required collection of {typeof(T).Name} is empty");
        }

        return items;
    }
}

/// <summary>
/// Чистая логика «умного» удаления под-сущности (permanent vs soft). Сам факт удаления из БД
/// делегируется capability-делегату, чтобы не тянуть <see cref="JoinRpg.Data.Write.Interfaces.IUnitOfWork"/>.
/// </summary>
internal static class EntityDeletion
{
    /// <returns><c>true</c>, если сущность удалена окончательно; <c>false</c> при soft-delete или <c>null</c>.</returns>
    public static bool SmartDelete<T>(T? field, Action<T> removePermanently) where T : class, IDeletableSubEntity
    {
        if (field == null)
        {
            return false;
        }

        if (field.CanBePermanentlyDeleted)
        {
            removePermanently(field);
            return true;
        }

        field.IsActive = false;
        return false;
    }
}
