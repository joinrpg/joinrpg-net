using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.Dal.Impl.Repositories;

internal static class CharacterPredicates
{
    internal static Expression<Func<Character, bool>> Hot() => character => character.IsHot && character.IsActive && character.ApprovedClaim == null;


    internal static Expression<Func<Character, bool>> ByGroup(IReadOnlyCollection<CharacterGroupIdentification> groups)
    {
        if (groups.Count == 0)
        {
            return character => false;
        }

        var projectId = groups.EnsureSameProject().First().ProjectId;
        var groupIntIds = groups.Select(x => x.CharacterGroupId).ToArray();

        return character => character.ProjectId == projectId
            && groupIntIds.Any(id => ("," + character.ParentGroupsImpl.ListIds + ",").Contains("," + id.ToString() + ","));
    }
}
