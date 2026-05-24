using JoinRpg.Helpers;

namespace JoinRpg.Domain;

public static class ParentGroupCalculateExtensions
{
    [Obsolete("Pass ProjectInfo")]
    public static IEnumerable<CharacterGroup> GetParentGroupsToTop(this CharacterGroup? target)
    {
        return target?.ParentGroups.SelectMany(g => g.FlatTree(gr => gr.ParentGroups))
                   .OrderBy(g => g.CharacterGroupId)
                   .Distinct() ?? [];
    }

    [Obsolete("Pass ProjectInfo")]
    public static IEnumerable<CharacterGroup> GetParentGroupsToTop(this Character? target)
    {
        return target?.Groups.SelectMany(g => g.FlatTree(gr => gr.ParentGroups))
                   .OrderBy(g => g.CharacterGroupId)
                   .Distinct() ?? [];
    }

    [Obsolete("Pass ProjectInfo")]
    public static IReadOnlyCollection<CharacterGroupIdentification> GetParentGroupIdsToTop(this Character? target)
    {
        if (target == null)
        {
            return [];
        }
        return [.. target.Groups.SelectMany(g => g.FlatTree(gr => gr.ParentGroups)).Select(x => x.GetId()).Order().Distinct()];
    }
}
