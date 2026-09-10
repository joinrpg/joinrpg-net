using JoinRpg.Helpers;

namespace JoinRpg.Domain;

public static class ParentGroupCalculateExtensions
{
    [Obsolete("Pass ProjectInfo")]
    public static IEnumerable<CharacterGroup> GetParentGroupsToTop(this Character? target)
    {
        return target?.Groups.SelectMany(g => g.FlatTree(gr => gr.ParentGroups))
                   .OrderBy(g => g.CharacterGroupId)
                   .Distinct() ?? [];
    }

}
