using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// «Работа по заявке остановлена» — правило только для архивных проектов.
/// </summary>
/// <remarks>
/// До перевода на доменные сущности условие было записано как <c>claim.Project.Active</c>.
/// <c>Project.Active == false</c> — это ровно <see cref="ProjectLifecycleStatus.Archived"/>
/// (см. <c>ProjectLoaderCommon.CreateStatus</c>), поэтому здесь проверяются оба состояния
/// проекта: правило должно молчать на любом неархивном.
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

    [Theory]
    [InlineData(ProjectLifecycleStatus.ActiveClaimsOpen)]
    [InlineData(ProjectLifecycleStatus.ActiveClaimsClosed)]
    public void ActiveProjectIsNotChecked(ProjectLifecycleStatus projectStatus)
    {
        Problems(projectStatus).ShouldBeEmpty();
    }

    [Fact]
    public void NotApprovedClaimIsNotChecked()
    {
        Problems(ProjectLifecycleStatus.Archived, status: ClaimStatus.Discussed).ShouldBeEmpty();
    }

    [Fact]
    public void FreshClaimIsNotChecked()
    {
        Problems(ProjectLifecycleStatus.Archived, createdDaysAgo: 1).ShouldBeEmpty();
    }

    [Fact]
    public void ArchivedProjectWithNeverAnsweredClaimIsError()
    {
        var problems = Problems(ProjectLifecycleStatus.Archived, lastMasterAnswerDaysAgo: null);

        problems.ShouldHaveSingleItem();
        problems.Single().ProblemType.ShouldBe(ClaimProblemType.ClaimNeverAnswered);
        problems.Single().Severity.ShouldBe(ProblemSeverity.Error);
    }

    [Fact]
    public void ArchivedProjectWithAnswerOlderThanSixtyDaysIsHint()
    {
        var problems = Problems(ProjectLifecycleStatus.Archived, lastMasterAnswerDaysAgo: 70);

        problems.ShouldHaveSingleItem();
        problems.Single().ProblemType.ShouldBe(ClaimProblemType.ClaimWorkStopped);
        problems.Single().Severity.ShouldBe(ProblemSeverity.Hint);
    }

    [Fact]
    public void ArchivedProjectWithRecentAnswerIsNotAProblem()
    {
        Problems(ProjectLifecycleStatus.Archived, lastMasterAnswerDaysAgo: 10).ShouldBeEmpty();
    }
}
