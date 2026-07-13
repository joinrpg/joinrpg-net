using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain;

public static class FieldExtensions
{
    [Obsolete("Pass ProjectInfo")]
    public static bool IsAvailableForTarget(this ProjectFieldInfo field, Character? target)
    {
        ArgumentNullException.ThrowIfNull(field);

        var isNpc = target?.CharacterType == CharacterType.NonPlayer;

        var targetGroups = target?.GetParentGroupIdsToTop().ToList();

        return IsAvailableForTargetCore(field, isNpc, targetGroups);
    }

    public static bool IsAvailableForTarget(this ProjectFieldInfo field, Character? target, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(field);

        var isNpc = target?.CharacterType == CharacterType.NonPlayer;

        var targetGroups = target?.GetParentGroupIdsToTop(projectInfo).ToList();

        return IsAvailableForTargetCore(field, isNpc, targetGroups);
    }

    public static bool IsAvailableForTarget(this ProjectFieldInfo field, CharacterItem target)
    {
        ArgumentNullException.ThrowIfNull(field);

        var isNpc = target.Character.CharacterType == CharacterType.NonPlayer;

        return IsAvailableForTargetCore(field, isNpc, target.ParentGroups);
    }

    private static bool IsAvailableForTargetCore(ProjectFieldInfo field, bool isNpc, IEnumerable<CharacterGroupIdentification>? targetGroups)
    {
        return field.IsActive
                  && (field.BoundTo == FieldBoundTo.Claim || field.ValidForNpc || !isNpc)
                  && (field.GroupsAvailableForIds.Count == 0 || (targetGroups?.Intersect(field.GroupsAvailableForIds).Any() ?? false));
    }

}
