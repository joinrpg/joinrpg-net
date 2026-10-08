using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems;

/// <summary>
/// Проблемы заявки, посчитанные по доменным сущностям (ADR013).
/// </summary>
/// <remarks>
/// Зеркало <see cref="ICharacterProblemValidator"/> для стороны заявки. <see cref="ProjectInfo"/>
/// параметром не передаётся — он приходит внутри <see cref="ClaimInfo"/>.
/// </remarks>
public interface IClaimProblemValidator
{
    IEnumerable<ClaimProblem> Validate(ClaimInfo context, ProblemSeverity minimalSeverity = ProblemSeverity.Hint);

    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context);

    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context, IEnumerable<ProjectFieldIdentification> fields);

    IEnumerable<FieldRelatedProblem> ValidateFieldOnly(ClaimInfo context, ProjectFieldIdentification fieldId)
        => ValidateFieldsOnly(context, [fieldId]);
}
