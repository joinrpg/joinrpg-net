using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems;

/// <summary>
/// Правило поиска проблем персонажа поверх доменного агрегата (ADR013).
/// </summary>
/// <remarks>
/// Парное правило к <see cref="IClaimProblemFilter"/>. <see cref="ProjectInfo"/> отдельным
/// параметром не передаётся: у агрегата он есть внутри (<see cref="CharacterInfo.ProjectInfo"/>),
/// а второй канал тех же метаданных означал бы, что их можно передать несогласованными.
/// </remarks>
public interface ICharacterProblemFilter
{
    IEnumerable<ClaimProblem> GetProblems(CharacterInfo character);
}
