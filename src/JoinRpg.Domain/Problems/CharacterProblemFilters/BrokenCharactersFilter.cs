using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems.CharacterProblemFilters;

internal class BrokenCharactersFilter : ICharacterProblemFilter
{
    public IEnumerable<ClaimProblem> GetProblems(CharacterInfo character)
    {
        var groups = character.ParentGroupsToTop.Where(g => g.IsActive && !g.IsSpecial).ToArray();
        if (groups.Length == 0)
        {
            yield return new ClaimProblem(ClaimProblemType.NoParentGroup, ProblemSeverity.Fatal);
        }
        foreach (var groupProblem in groups.SelectMany(GetProblemFroGroup))
        {
            yield return groupProblem;
        }
    }

    private IEnumerable<ClaimProblem> GetProblemFroGroup(CharacterGroupInfo group)
    {
        if (group.IsRoot)
        {
            yield break;
        }
        if (!group.DirectParentGroupIds.Any() || group.DirectParentGroupIds.Any(id => id == group.Id))
        {
            yield return new ClaimProblem(ClaimProblemType.GroupIsBroken, ProblemSeverity.Fatal, group.Name);
        }
    }
}
