using JoinRpg.DataModel;
using JoinRpg.Domain.ClaimProblemFilters;

namespace JoinRpg.Domain.CharacterProblemFilters;

internal class BrokenCharactersFilter : IProblemFilter<Character>
{
    public IEnumerable<ClaimProblem> GetProblems(Character character)
    {
        var groups = character.GetParentGroupsToTop().Where(g => g.IsActive && !g.IsSpecial).ToArray();
        if (!groups.Any())
        {
            yield return new ClaimProblem(ClaimProblemType.NoParentGroup, ProblemSeverity.Fatal);
        }
        foreach (var groupProblem in groups.SelectMany(GetProblemFroGroup))
        {
            yield return groupProblem;
        }
    }

    private IEnumerable<ClaimProblem> GetProblemFroGroup(CharacterGroup group)
    {
        if (group.IsRoot)
        {
            yield break;
        }
        if (!group.ParentCharacterGroupIds.Any() || group.ParentCharacterGroupIds.Any(id => id == group.CharacterGroupId))
        {
            yield return new ClaimProblem(ClaimProblemType.GroupIsBroken, ProblemSeverity.Fatal, group.CharacterGroupName);
        }
    }
}
