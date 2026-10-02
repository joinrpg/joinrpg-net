using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems;

/// <summary>
/// Проблемы персонажа, посчитанные по доменному агрегату (ADR013).
/// </summary>
/// <remarks>
/// Отдельный интерфейс, а не <c>IProblemValidator&lt;CharacterInfo&gt;</c>: метаданные проекта
/// лежат внутри агрегата, поэтому <see cref="ProjectInfo"/> параметром не нужен. Проблемы заявки
/// пока считаются старым generic-валидатором поверх EF-сущности.
/// </remarks>
public interface ICharacterProblemValidator
{
    IEnumerable<ClaimProblem> Validate(CharacterInfo character, ProblemSeverity minimalSeverity = ProblemSeverity.Hint);

    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(CharacterInfo character);

    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(CharacterInfo character, IEnumerable<ProjectFieldIdentification> fields);

    IEnumerable<FieldRelatedProblem> ValidateFieldOnly(CharacterInfo character, ProjectFieldIdentification fieldId)
        => ValidateFieldsOnly(character, [fieldId]);
}
