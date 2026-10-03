using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Финансовые проблемы заявки — все четыре правила фильтра.
/// </summary>
/// <remarks>
/// Соответствие прежним выражениям поверх EF-сущности:
/// <c>claim.ClaimBalance()</c> (сумма подтверждённых операций) — это <c>balance.FeePaid</c>,
/// <c>claim.ClaimTotalFee(projectInfo)</c> — <c>balance.TotalFee</c>, а
/// <c>claim.ClaimPaidInFull(projectInfo)</c> — <c>balance.FeeDue &lt;= 0</c>. Взнос задаётся через
/// <c>CurrentFee</c>: он фиксирует базовую сумму и не зависит от расписания взносов проекта.
/// </remarks>
public class FinanceProblemsFilterTest : ClaimProblemFilterTestBase
{
    private FinanceProblemsFilter Filter { get; } = new FinanceProblemsFilter();

    private IReadOnlyCollection<ClaimProblem> Problems(
        int totalFee,
        int feePaid,
        ClaimStatus status = ClaimStatus.Approved,
        bool requireModeration = false,
        bool warnOnOverPayment = true)
    {
        // Настройка живёт в сущности проекта, поэтому меняем её там и пересобираем метаданные:
        // ProjectInfo.ProjectFinanceSettings — get-only, через `with` его не подменить.
        Mock.Project.Details.FinanceWarnOnOverPayment = warnOnOverPayment;
        Mock.ReInitProjectInfo();
        var projectInfo = Mock.ProjectInfo;

        return [.. Filter.GetProblems(MakeContext(
            claim => claim with
            {
                Status = status,
                Finance = claim.Finance with
                {
                    FixedFee = totalFee,
                    FeePaid = feePaid,
                    AccommodationFee = 0,
                    OperationsRequireModeration = requireModeration,
                },
            },
            projectInfo))];
    }

    private static bool Has(IReadOnlyCollection<ClaimProblem> problems, ClaimProblemType type)
        => problems.Any(p => p.ProblemType == type);

    [Fact]
    public void PendingModerationIsWarning()
    {
        var problems = Problems(totalFee: 1000, feePaid: 1000, requireModeration: true);

        problems.ShouldContain(p =>
            p.ProblemType == ClaimProblemType.FinanceModerationRequired && p.Severity == ProblemSeverity.Warning);
    }

    [Fact]
    public void NoModerationProblemWhenNothingWaits()
    {
        Has(Problems(totalFee: 1000, feePaid: 1000), ClaimProblemType.FinanceModerationRequired).ShouldBeFalse();
    }

    /// <summary>Уплачено больше начисленного — переплата.</summary>
    [Fact]
    public void OverPaymentIsError()
    {
        var problems = Problems(totalFee: 1000, feePaid: 1500);

        problems.ShouldContain(p =>
            p.ProblemType == ClaimProblemType.TooManyMoney && p.Severity == ProblemSeverity.Error);
    }

    /// <summary>
    /// Если проект просил не жаловаться на переплату — не жалуемся. Остальные правила при этом
    /// продолжают работать.
    /// </summary>
    [Fact]
    public void OverPaymentIsIgnoredWhenProjectDoesNotWarnOnIt()
    {
        Has(Problems(totalFee: 1000, feePaid: 1500, warnOnOverPayment: false), ClaimProblemType.TooManyMoney)
            .ShouldBeFalse();
    }

    [Fact]
    public void ExactPaymentIsNotOverPayment()
    {
        Has(Problems(totalFee: 1000, feePaid: 1000), ClaimProblemType.TooManyMoney).ShouldBeFalse();
    }

    /// <summary>Заплатили, но не до конца.</summary>
    [Fact]
    public void PartialPaymentIsHint()
    {
        var problems = Problems(totalFee: 1000, feePaid: 400);

        problems.ShouldContain(p =>
            p.ProblemType == ClaimProblemType.FeePaidPartially && p.Severity == ProblemSeverity.Hint);
    }

    /// <summary>Не заплатили вообще — это не «уплачено частично».</summary>
    [Fact]
    public void NothingPaidIsNotPartialPayment()
    {
        Has(Problems(totalFee: 1000, feePaid: 0), ClaimProblemType.FeePaidPartially).ShouldBeFalse();
    }

    [Fact]
    public void FullyPaidIsNotPartialPayment()
    {
        Has(Problems(totalFee: 1000, feePaid: 1000), ClaimProblemType.FeePaidPartially).ShouldBeFalse();
    }

    /// <summary>Деньги по заявке, решения по которой ещё нет.</summary>
    [Fact]
    public void PaymentOnClaimInDiscussionIsWarning()
    {
        var problems = Problems(totalFee: 1000, feePaid: 100, status: ClaimStatus.Discussed);

        problems.ShouldContain(p =>
            p.ProblemType == ClaimProblemType.UnApprovedClaimPayment && p.Severity == ProblemSeverity.Warning);
    }

    [Fact]
    public void UnpaidClaimInDiscussionIsNotAProblem()
    {
        Has(Problems(totalFee: 1000, feePaid: 0, status: ClaimStatus.Discussed), ClaimProblemType.UnApprovedClaimPayment)
            .ShouldBeFalse();
    }

    [Fact]
    public void PaymentOnApprovedClaimIsNotUnApprovedPayment()
    {
        Has(Problems(totalFee: 1000, feePaid: 100), ClaimProblemType.UnApprovedClaimPayment).ShouldBeFalse();
    }
}
