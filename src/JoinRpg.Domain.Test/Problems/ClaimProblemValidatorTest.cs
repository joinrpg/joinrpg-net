using JoinRpg.Domain.Problems;
using JoinRpg.Domain.Problems.CommonProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Отбор для страницы «Игрокам нужно заполнить»: туда идут требуемые данные профиля, а проблемы
/// работы мастеров с заявкой и подсказки по рекомендованным контактам — нет.
/// </summary>
public class ClaimProblemValidatorTest : ClaimProblemFilterTestBase
{
    private static readonly ProfileRelatedProblem RequiredProfileProblem = new(ClaimProblemType.MissingPassport, ProblemSeverity.Warning);
    private static readonly ProfileRelatedProblem RecommendedProfileProblem = new(ClaimProblemType.MissingPhone, ProblemSeverity.Hint);
    private static readonly ClaimProblem MasterProblem = new(ClaimProblemType.InvalidResponsibleMaster, ProblemSeverity.Error);

    private readonly ClaimProblemValidator validator = new(
        [new StubFilter([MasterProblem, RequiredProfileProblem, RecommendedProfileProblem])],
        [new FieldNotSetFilter()]);

    [Fact]
    public void PlayerToFillContainsOnlyRequiredProfileProblems()
        => ((IClaimProblemValidator)validator).ValidatePlayerToFill(MakeContext()).ShouldBe([RequiredProfileProblem]);

    private sealed class StubFilter(IReadOnlyCollection<ClaimProblem> problems) : IClaimProblemFilter
    {
        public IEnumerable<ClaimProblem> GetProblems(ClaimInfo context) => problems;
    }
}
