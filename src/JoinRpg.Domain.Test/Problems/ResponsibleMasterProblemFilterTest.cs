using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Ответственный за заявку должен быть мастером этого проекта.
/// </summary>
public class ResponsibleMasterProblemFilterTest : ClaimProblemFilterTestBase
{
    private ResponsibleMasterProblemFilter Filter { get; } = new ResponsibleMasterProblemFilter();

    [Fact]
    public void MasterOfThisProjectIsFine()
    {
        Filter.GetProblems(MakeContext()).ShouldBeEmpty();
    }

    [Fact]
    public void UserWithoutMasterAccessIsError()
    {
        // 999 нет среди мастеров мока, значит HasMasterAccess вернёт false.
        var problems = Filter.GetProblems(MakeContext(
            claim => claim with { ResponsibleMasterId = new UserIdentification(999) }));

        problems.ShouldHaveSingleItem();
        problems.Single().ProblemType.ShouldBe(ClaimProblemType.InvalidResponsibleMaster);
        problems.Single().Severity.ShouldBe(ProblemSeverity.Error);
    }

    /// <summary>
    /// Ответственным может быть и второй мастер проекта, не только тот, кого подставляет мок.
    /// </summary>
    [Fact]
    public void AnotherMasterOfSameProjectIsFine()
    {
        var anotherMaster = Mock.CreateMaster();
        Mock.ReInitProjectInfo();

        Filter.GetProblems(MakeContext(
            claim => claim with { ResponsibleMasterId = new UserIdentification(anotherMaster.UserId) }))
            .ShouldBeEmpty();
    }
}
