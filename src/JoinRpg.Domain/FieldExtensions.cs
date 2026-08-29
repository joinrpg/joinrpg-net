using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain;

public static class FieldExtensions
{
    [Obsolete("Pass ProjectInfo")]
    public static bool IsAvailableForTarget(this ProjectFieldInfo field, Character? target)
    {
        ArgumentNullException.ThrowIfNull(field);

        // Группы считаются обходом EF-навигации, а не по ProjectInfo — отсюда и Obsolete.
        return IsAvailableForTargetCore(field, target?.CharacterType, target?.GetParentGroupIdsToTop());
    }

    public static bool IsAvailableForTarget(this ProjectFieldInfo field, Character? target, ProjectInfo projectInfo)
        => field.IsAvailableForTarget(
            target is null ? null : new CharacterItem(target, [.. target.GetParentGroupIdsToTop(projectInfo)]));

    /// <summary>
    /// Доступно ли поле персонажу. <paramref name="target"/> == <c>null</c> означает «персонажа нет»:
    /// тогда доступны только поля без ограничения по группам.
    /// </summary>
    public static bool IsAvailableForTarget(this ProjectFieldInfo field, IFieldAvailabilityTarget? target)
    {
        ArgumentNullException.ThrowIfNull(field);

        return IsAvailableForTargetCore(field, target?.CharacterType, target?.ParentGroupIdsToTop);
    }

    private static bool IsAvailableForTargetCore(
        ProjectFieldInfo field,
        CharacterType? characterType,
        IEnumerable<CharacterGroupIdentification>? targetGroups)
    {
        return field.IsActive
                  && (field.BoundTo == FieldBoundTo.Claim || field.ValidForNpc || characterType != CharacterType.NonPlayer)
                  && (field.GroupsAvailableForIds.Count == 0 || (targetGroups?.Intersect(field.GroupsAvailableForIds).Any() ?? false));
    }

}
