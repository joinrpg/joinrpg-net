using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// «Заявка без ответа» / «обсуждение остановилось» / «нет решения» — все пороги правила.
/// </summary>
/// <remarks>
/// Правило смотрит на две независимые шкалы: давность последнего мастерского ответа (7 и 14 дней)
/// и давность самой заявки (14, 30 и 60 дней), плюс отсечку «подана меньше двух дней назад».
/// Поэтому в каждом тесте фиксируются обе даты.
/// </remarks>
public class NotAnsweredClaimTest : ClaimProblemFilterTestBase
{
    private NotAnsweredClaim Filter { get; } = new NotAnsweredClaim();

    private IReadOnlyCollection<ClaimProblem> Problems(
        int createdDaysAgo,
        int? lastMasterAnswerDaysAgo,
        ClaimStatus status = ClaimStatus.Discussed)
        => [.. Filter.GetProblems(MakeContext(claim => claim with
        {
            Status = status,
            CreateDate = DateTime.UtcNow.AddDays(-createdDaysAgo),
            LastVisibleMasterCommentAt = lastMasterAnswerDaysAgo is int days
                ? DateTimeOffset.Now.AddDays(-days)
                : null,
        }))];

    private static ProblemSeverity? SeverityOf(IReadOnlyCollection<ClaimProblem> problems, ClaimProblemType type)
        => problems.SingleOrDefault(p => p.ProblemType == type)?.Severity;

    [Fact]
    public void NotDiscussedClaimIsNotChecked()
    {
        Problems(createdDaysAgo: 100, lastMasterAnswerDaysAgo: null, status: ClaimStatus.Approved)
            .ShouldBeEmpty();
    }

    /// <summary>Только что поданную заявку не трогаем, даже если мастер ещё не ответил.</summary>
    [Fact]
    public void FreshClaimIsNotChecked()
    {
        Problems(createdDaysAgo: 1, lastMasterAnswerDaysAgo: null).ShouldBeEmpty();
    }

    [Fact]
    public void ClaimOlderThanTwoDaysWithoutAnyAnswerIsError()
    {
        var problems = Problems(createdDaysAgo: 3, lastMasterAnswerDaysAgo: null);

        SeverityOf(problems, ClaimProblemType.ClaimNeverAnswered).ShouldBe(ProblemSeverity.Error);
        SeverityOf(problems, ClaimProblemType.ClaimNoDecision).ShouldBeNull();
    }

    [Fact]
    public void RecentlyAnsweredClaimHasNoDiscussionProblem()
    {
        var problems = Problems(createdDaysAgo: 3, lastMasterAnswerDaysAgo: 1);

        SeverityOf(problems, ClaimProblemType.ClaimNeverAnswered).ShouldBeNull();
        SeverityOf(problems, ClaimProblemType.ClaimDiscussionStopped).ShouldBeNull();
    }

    /// <summary>Мастер молчит дольше семи дней, но меньше четырнадцати — предупреждение.</summary>
    [Fact]
    public void NoAnswerForMoreThanSevenDaysIsWarning()
    {
        SeverityOf(Problems(createdDaysAgo: 10, lastMasterAnswerDaysAgo: 8), ClaimProblemType.ClaimDiscussionStopped)
            .ShouldBe(ProblemSeverity.Warning);
    }

    /// <summary>Дольше четырнадцати дней — уже ошибка, и предупреждение не дублируется.</summary>
    [Fact]
    public void NoAnswerForMoreThanFourteenDaysIsError()
    {
        var problems = Problems(createdDaysAgo: 20, lastMasterAnswerDaysAgo: 15);

        problems.Count(p => p.ProblemType == ClaimProblemType.ClaimDiscussionStopped).ShouldBe(1);
        SeverityOf(problems, ClaimProblemType.ClaimDiscussionStopped).ShouldBe(ProblemSeverity.Error);
    }

    [Fact]
    public void ClaimYoungerThanFourteenDaysHasNoNoDecisionProblem()
    {
        SeverityOf(Problems(createdDaysAgo: 10, lastMasterAnswerDaysAgo: 1), ClaimProblemType.ClaimNoDecision)
            .ShouldBeNull();
    }

    [Fact]
    public void ClaimOlderThanFourteenDaysWithoutDecisionIsHint()
    {
        SeverityOf(Problems(createdDaysAgo: 20, lastMasterAnswerDaysAgo: 1), ClaimProblemType.ClaimNoDecision)
            .ShouldBe(ProblemSeverity.Hint);
    }

    [Fact]
    public void ClaimOlderThanThirtyDaysWithoutDecisionIsWarning()
    {
        SeverityOf(Problems(createdDaysAgo: 40, lastMasterAnswerDaysAgo: 1), ClaimProblemType.ClaimNoDecision)
            .ShouldBe(ProblemSeverity.Warning);
    }

    [Fact]
    public void ClaimOlderThanSixtyDaysWithoutDecisionIsError()
    {
        SeverityOf(Problems(createdDaysAgo: 70, lastMasterAnswerDaysAgo: 1), ClaimProblemType.ClaimNoDecision)
            .ShouldBe(ProblemSeverity.Error);
    }

    /// <summary>
    /// Шкалы независимы: по старой заявке, которой мастер давно не отвечал, приходят обе проблемы.
    /// </summary>
    [Fact]
    public void OldAndUnansweredClaimGetsBothProblems()
    {
        var problems = Problems(createdDaysAgo: 70, lastMasterAnswerDaysAgo: 30);

        SeverityOf(problems, ClaimProblemType.ClaimDiscussionStopped).ShouldBe(ProblemSeverity.Error);
        SeverityOf(problems, ClaimProblemType.ClaimNoDecision).ShouldBe(ProblemSeverity.Error);
    }
}
