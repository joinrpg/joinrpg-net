using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain;
public static class ParentGroupCalculateExtensions
{
    public static IEnumerable<CharacterGroup> GetParentGroupsToTop(this IClaimSource? target)
    {
        return target?.ParentGroups.SelectMany(g => g.FlatTree(gr => gr.ParentGroups))
                   .OrderBy(g => g.CharacterGroupId)
                   .Distinct() ?? Enumerable.Empty<CharacterGroup>();
    }

    public static IEnumerable<CharacterGroup> GetChildrenGroupsRecursive(
        this CharacterGroup target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return target.ChildGroups.SelectMany(g => g.FlatTree(gr => gr.ChildGroups)).Distinct();
    }

    public static IEnumerable<CharacterGroup> GetOrderedChildrenGroupsRecursive(
        this CharacterGroup target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return target.ChildGroups.SelectMany(g => g.FlatTree(gr => gr.GetOrderedChildGroups())).Distinct();
    }
}