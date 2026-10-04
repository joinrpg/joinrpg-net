using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems;

/// <summary>
/// Проблемы заявки, посчитанные по доменным сущностям (ADR013).
/// </summary>
/// <remarks>
/// Зеркало <see cref="ICharacterProblemValidator"/> для стороны заявки. <see cref="ProjectInfo"/>
/// параметром не передаётся — он приходит внутри <see cref="ClaimProblemContext"/>.
/// </remarks>
public interface IClaimProblemValidator
{
    IEnumerable<ClaimProblem> Validate(ClaimProblemContext context, ProblemSeverity minimalSeverity = ProblemSeverity.Hint);

    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimProblemContext context);

    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimProblemContext context, IEnumerable<ProjectFieldIdentification> fields);

    IEnumerable<FieldRelatedProblem> ValidateFieldOnly(ClaimProblemContext context, ProjectFieldIdentification fieldId)
        => ValidateFieldsOnly(context, [fieldId]);
}
