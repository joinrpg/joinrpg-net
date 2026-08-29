using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain;

public static class FieldExtensions
{
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

        return field.IsActive
                  && (field.BoundTo == FieldBoundTo.Claim || field.ValidForNpc || target?.CharacterType != CharacterType.NonPlayer)
                  && (field.GroupsAvailableForIds.Count == 0
                      || (target?.ParentGroupIdsToTop.Intersect(field.GroupsAvailableForIds).Any() ?? false));
    }
}
