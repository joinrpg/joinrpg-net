using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

/// <remarks>
/// Баланс считается один раз на все четыре правила — доменным
/// <see cref="ClaimBalanceExtensions.CalculateBalance(ClaimInCharacter, System.DateTime?)"/>,
/// без обращения к EF. Раньше каждое правило дёргало <c>ClaimBalance()</c> / <c>ClaimTotalFee()</c>
/// по отдельности, пересчитывая взнос за поля заново.
/// </remarks>
internal class FinanceProblemsFilter : IClaimProblemFilter
{
    public IEnumerable<ClaimProblem> GetProblems(ClaimInfo context)
    {
        var claim = context.Claim;

        // Соответствие прежним выражениям поверх EF-сущности:
        //   claim.ClaimBalance()                = сумма подтверждённых операций = balance.FeePaid
        //   claim.ClaimTotalFee(projectInfo)    = взнос + поля + проживание     = balance.TotalFee
        //   claim.ClaimPaidInFull(projectInfo)  = FeePaid >= TotalFee           = balance.FeeDue <= 0
        var balance = context.ClaimInCharacter.CalculateBalance();

        if (claim.Finance.OperationsRequireModeration)
        {
            yield return new ClaimProblem(ClaimProblemType.FinanceModerationRequired, ProblemSeverity.Warning);
        }

        // Было: ClaimTotalFee < ClaimBalance, то есть уплачено больше, чем начислено.
        if (balance.FeeDue < 0 && context.ProjectInfo.ProjectFinanceSettings.WarnOnOverPayment)
        {
            yield return new ClaimProblem(ClaimProblemType.TooManyMoney, ProblemSeverity.Error);
        }

        // Было: !ClaimPaidInFull && ClaimBalance > 0, то есть заплатили, но не до конца.
        if (balance.FeeDue > 0 && balance.FeePaid > 0)
        {
            yield return new ClaimProblem(ClaimProblemType.FeePaidPartially, ProblemSeverity.Hint);
        }

        // Было: IsInDiscussion && ClaimBalance > 0 — деньги по заявке, решения по которой нет.
        if (claim.IsInDiscussion && balance.FeePaid > 0)
        {
            yield return new ClaimProblem(ClaimProblemType.UnApprovedClaimPayment, ProblemSeverity.Warning);
        }
    }
}
