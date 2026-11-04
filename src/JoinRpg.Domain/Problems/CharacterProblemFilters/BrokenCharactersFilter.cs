using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems.CharacterProblemFilters;

/// <summary>
/// Структурные проблемы персонажа (нет активной родительской группы, сломанная группа).
/// Считаются и для заявок, чтобы проблемы утверждённой заявки были надмножеством проблем
/// персонажа. Флаги у этих типов не заявочные: <c>BlocksCheckIn</c> = false (сломанная
/// структура не останавливает регистрацию), игроку не видны, автоприём не блокируют —
/// это фиксирует тест-страж ClaimProblemVisibleToPlayerTest.
/// </summary>
internal class BrokenCharactersFilter : ICharacterProblemFilter, IClaimProblemFilter
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

    public IEnumerable<ClaimProblem> GetProblems(ClaimInfo context) => GetProblems(context.Character);

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
