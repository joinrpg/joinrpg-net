using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// «Работа по заявке остановлена» — правило для живых проектов.
/// </summary>
/// <remarks>
/// До перевода на доменные сущности условие было записано как <c>claim.Project.Active</c> и
/// работало наоборот: молчало на живых проектах и срабатывало в архиве, где разбираться с
/// заявкой уже незачем. Здесь проверяются оба живых состояния проекта и архив.
/// </remarks>
public class ClaimWorkStoppedTest : ClaimProblemFilterTestBase
{
    private ClaimWorkStopped Filter { get; } = new ClaimWorkStopped();

    private IReadOnlyCollection<ClaimProblem> Problems(
        ProjectLifecycleStatus projectStatus,
        int createdDaysAgo = 100,
        int? lastMasterAnswerDaysAgo = null,
        ClaimStatus status = ClaimStatus.Approved)
    {
        var projectInfo = Mock.ProjectInfo.WithChangedStatus(projectStatus);

        return [.. Filter.GetProblems(MakeContext(
            claim => claim with
            {
                Status = status,
                CreateDate = DateTime.UtcNow.AddDays(-createdDaysAgo),
                LastVisibleMasterCommentAt = lastMasterAnswerDaysAgo is int days
                    ? DateTimeOffset.Now.AddDays(-days)
                    : null,
            },
            projectInfo))];
    }

    /// <summary>
    /// Игра кончилась — разбираться, остановилась ли работа по заявке, незачем.
    /// </summary>
    [Fact]
    public void ArchivedProjectIsNotChecked()
    {
        Problems(ProjectLifecycleStatus.Archived, lastMasterAnswerDaysAgo: null).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ProjectLifecycleStatus.ActiveClaimsOpen)]
    [InlineData(ProjectLifecycleStatus.ActiveClaimsClosed)]
    public void NotApprovedClaimIsNotChecked(ProjectLifecycleStatus projectStatus)
    {
        Problems(projectStatus, status: ClaimStatus.Discussed).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ProjectLifecycleStatus.ActiveClaimsOpen)]
    [InlineData(ProjectLifecycleStatus.ActiveClaimsClosed)]
    public void FreshClaimIsNotChecked(ProjectLifecycleStatus projectStatus)
    {
        Problems(projectStatus, createdDaysAgo: 1).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ProjectLifecycleStatus.ActiveClaimsOpen)]
    [InlineData(ProjectLifecycleStatus.ActiveClaimsClosed)]
    public void NeverAnsweredClaimIsError(ProjectLifecycleStatus projectStatus)
    {
        var problems = Problems(projectStatus, lastMasterAnswerDaysAgo: null);

        problems.ShouldHaveSingleItem();
        problems.Single().ProblemType.ShouldBe(ClaimProblemType.ClaimNeverAnswered);
        problems.Single().Severity.ShouldBe(ProblemSeverity.Error);
    }

    [Fact]
    public void AnswerOlderThanSixtyDaysIsHint()
    {
        var problems = Problems(ProjectLifecycleStatus.ActiveClaimsOpen, lastMasterAnswerDaysAgo: 70);

        problems.ShouldHaveSingleItem();
        problems.Single().ProblemType.ShouldBe(ClaimProblemType.ClaimWorkStopped);
        problems.Single().Severity.ShouldBe(ProblemSeverity.Hint);
    }

    [Fact]
    public void RecentAnswerIsNotAProblem()
    {
        Problems(ProjectLifecycleStatus.ActiveClaimsOpen, lastMasterAnswerDaysAgo: 10).ShouldBeEmpty();
    }
}
