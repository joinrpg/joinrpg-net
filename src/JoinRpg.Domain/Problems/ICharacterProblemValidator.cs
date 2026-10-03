using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems;

/// <summary>
/// Проблемы персонажа, посчитанные по доменному агрегату (ADR013).
/// </summary>
/// <remarks>
/// Отдельный интерфейс на сторону персонажа, парный <see cref="IClaimProblemValidator"/>:
/// метаданные проекта лежат внутри агрегата, поэтому <see cref="ProjectInfo"/> параметром не
/// нужен. Общего generic-валидатора поверх EF-сущностей больше нет — проблемы заявки считаются
/// по доменным сущностям так же.
/// </remarks>
public interface ICharacterProblemValidator
{
    IEnumerable<ClaimProblem> Validate(CharacterInfo character, ProblemSeverity minimalSeverity = ProblemSeverity.Hint);

    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(CharacterInfo character);

    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(CharacterInfo character, IEnumerable<ProjectFieldIdentification> fields);

    IEnumerable<FieldRelatedProblem> ValidateFieldOnly(CharacterInfo character, ProjectFieldIdentification fieldId)
        => ValidateFieldsOnly(character, [fieldId]);
}
