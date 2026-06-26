using System.Data.Entity.Validation;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Чистое ядро проставления аудита (created/updated). Единый источник истины для
/// <see cref="DbServiceImplBase"/> и extension-методов контекстов операций над проектом.
/// </summary>
internal static class EntityAudit
{
    public static void MarkCreated(ICreatedUpdatedTrackedForEntity entity, DateTime now, int userId)
    {
        entity.UpdatedAt = entity.CreatedAt = now;
        entity.UpdatedById = entity.CreatedById = userId;
    }

    public static void MarkChanged(ICreatedUpdatedTrackedForEntity entity, DateTime now, int userId)
    {
        entity.UpdatedAt = now;
        entity.UpdatedById = userId;
    }
}

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
            throw new DbEntityValidationException();
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

internal static class ProjectInfoValidationExtensions
{
    public static int[] ValidateCharacterGroupList(
        this ProjectInfo projectInfo,
        IReadOnlyCollection<CharacterGroupIdentification> groupIds,
        bool ensureNotSpecial = false)
    {
        foreach (var g in groupIds)
        {
            if (g.ProjectId != projectInfo.ProjectId)
            {
                throw new ArgumentException("Нельзя смешивать разные проекты в запросе!", nameof(groupIds));
            }
        }

        var missing = groupIds
            .Where(id => !projectInfo.Groups.ContainsKey(id))
            .ToArray();

        if (missing.Length != 0)
        {
            var missingIds = string.Join(", ", missing.Select(m => m.CharacterGroupId));
            throw new Exception($"Groups {missingIds} doesn't belong to project");
        }

        if (ensureNotSpecial && groupIds.Any(id => projectInfo.Groups[id].IsSpecial))
        {
            throw new DbEntityValidationException();
        }

        return [.. groupIds.Select(g => g.CharacterGroupId)];
    }
}
