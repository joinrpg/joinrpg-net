using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Валидатор проблем заявки, который проблем не находит.
/// </summary>
/// <remarks>
/// Настоящий валидатор собирается из десятка правил и ходит за данными, которых у мока нет;
/// проверяемым здесь операциям важно лишь то, что проблем нет. Тесты на сами правила живут в
/// <c>JoinRpg.Domain.Test</c>.
/// </remarks>
internal sealed class FakeProblemValidator : IClaimProblemValidator
{
    public IEnumerable<ClaimProblem> Validate(
        ClaimInfo context, ProblemSeverity minimalSeverity = ProblemSeverity.Hint) => [];

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(
        ClaimInfo context, IEnumerable<ProjectFieldIdentification> fields) => [];

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context) => [];
}
