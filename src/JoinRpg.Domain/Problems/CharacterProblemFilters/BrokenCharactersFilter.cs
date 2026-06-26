using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems.CharacterProblemFilters;

internal class BrokenCharactersFilter : IProblemFilter<Character>
{
    public IEnumerable<ClaimProblem> GetProblems(Character character, ProjectInfo projectInfo)
    {
        var groups = character.GetParentGroupsToTop(projectInfo).Where(g => g.IsActive && !g.IsSpecial).ToArray();
        if (!groups.Any())
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
