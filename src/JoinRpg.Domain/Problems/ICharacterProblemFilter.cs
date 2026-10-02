using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems;

/// <summary>
/// Правило поиска проблем персонажа поверх доменного агрегата (ADR013).
/// </summary>
/// <remarks>
/// Не <c>IProblemFilter&lt;CharacterInfo&gt;</c>: тот принимает <see cref="ProjectInfo"/> отдельным
/// параметром, а у агрегата он есть внутри (<see cref="CharacterInfo.ProjectInfo"/>) — второй канал
/// тех же метаданных означал бы, что их можно передать несогласованными.
/// </remarks>
public interface ICharacterProblemFilter
{
    IEnumerable<ClaimProblem> GetProblems(CharacterInfo character);
}
