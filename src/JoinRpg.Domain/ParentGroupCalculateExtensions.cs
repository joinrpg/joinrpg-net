using JoinRpg.Helpers;

namespace JoinRpg.Domain;
public static class ParentGroupCalculateExtensions
{
    public static IEnumerable<CharacterGroup> GetParentGroupsToTop(this IWorldObject? target)
    {
        return target?.ParentGroups.SelectMany(g => g.FlatTree(gr => gr.ParentGroups))
                   .OrderBy(g => g.CharacterGroupId)
                   .Distinct() ?? [];
    }

    public static IReadOnlyCollection<CharacterGroupIdentification> GetParentGroupIdsToTop(this IWorldObject? target)
    {
        if (target == null)
        {
            return [];
        }
        return [.. target.ParentGroups.SelectMany(g => g.FlatTree(gr => gr.ParentGroups)).Select(x => x.GetId()).Order().Distinct()];
    }

    public static IEnumerable<CharacterGroup> GetIntrestingGroupsForDisplayToTop(this Character character)
        => character.GetParentGroupsToTop().Where(g => !g.IsRoot && g.IsActive && (!g.IsSpecial || g.ParentGroups.All(g => !g.IsRoot)));

    public static IEnumerable<CharacterGroup> GetChildrenGroupsRecursive(
        this CharacterGroup target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return target.ChildGroups.SelectMany(g => g.FlatTree(gr => gr.ChildGroups)).Distinct();
    }

    [Obsolete]
    public static int[] GetChildrenGroupsIdRecursiveIncludingThis(this CharacterGroup target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return [.. target.GetChildrenGroupsRecursive().Select(g => g.CharacterGroupId), target.CharacterGroupId];
    }

    public static IEnumerable<CharacterGroupIdentification> GetChildrenGroupsIdentificationRecursiveIncludingThis(this CharacterGroup target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return [.. target.GetChildrenGroupsRecursive().Select(g => g.GetId()), target.GetId()];
    }

    public static IEnumerable<CharacterGroup> GetOrderedChildrenGroupsRecursive(
        this CharacterGroup target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return target.GetOrderedChildGroups().SelectMany(g => g.FlatTree(gr => gr.GetOrderedChildGroups())).Distinct();
    }
}
