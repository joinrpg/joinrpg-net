using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Валидатор проблем, который проблем не находит.
/// </summary>
/// <remarks>
/// Настоящий валидатор собирается из десятка правил и ходит за данными, которых у мока нет;
/// проверяемым здесь операциям важно лишь то, что проблем нет. Тесты на сами правила живут в
/// <c>JoinRpg.Domain.Test</c>.
/// </remarks>
internal sealed class FakeProblemValidator<TObject> : IProblemValidator<TObject>
    where TObject : IFieldContainter
{
    public IEnumerable<ClaimProblem> Validate(
        TObject claim, ProjectInfo projectInfo, ProblemSeverity minimalSeverity = ProblemSeverity.Hint) => [];

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(
        TObject claim, ProjectInfo projectInfo, IEnumerable<ProjectFieldIdentification> fields) => [];

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(TObject claim, ProjectInfo projectInfo) => [];
}
