using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain;

public static class FinanceExtensions
{
    /// <summary>
    /// Returns project fee for a specified date for claim
    /// </summary>
    private static int ProjectFeeForDate(this Claim claim, ProjectInfo projectInfo, DateTime? operationDate)
        => projectInfo.ProjectFinanceSettings.GetFeeForDate(
            operationDate ?? DateTime.UtcNow,
            claim.PreferentialFeeUser);

    /// <summary>
    /// Returns total sum of claim fee and all finance operations
    /// </summary>
    private static int ClaimTotalFee(this Claim claim, DateTime operationDate, int? fieldsFee, ProjectInfo projectInfo)
        => claim.ClaimCurrentFee(operationDate, fieldsFee, projectInfo);

    /// <summary>
    /// Returns total sum of claim fee and all finance operations using current date
    /// </summary>
    [Obsolete("CalculateClaimBalance")]
    public static int ClaimTotalFee(this Claim claim, ProjectInfo projectInfo, int? fieldsFee = null)
        => claim.ClaimTotalFee(DateTime.UtcNow, fieldsFee, projectInfo);

    /// <summary>
    /// Returns base fee (taken from project settings or claim's property CurrentFee)
    /// </summary>
    public static int BaseFee(this Claim claim, ProjectInfo projectInfo, DateTime? operationDate = null)
        => claim.CurrentFee ?? claim.ProjectFeeForDate(projectInfo, operationDate);

    /// <summary>
    /// Returns actual fee for a claim (as a sum of claim fee and fields fee) using current date
    /// </summary>
    public static int ClaimCurrentFee(this Claim claim, int? fieldsFee, ProjectInfo projectInfo)
        => claim.ClaimCurrentFee(DateTime.UtcNow, fieldsFee, projectInfo);

    /// <summary>
    /// Returns actual fee for a claim (as a sum of claim fee and fields fee)
    /// </summary>
    private static int ClaimCurrentFee(this Claim claim, DateTime operationDate, int? fieldsFee, ProjectInfo projectInfo)
    {
        return claim.BaseFee(projectInfo, operationDate)
               + claim.ClaimFieldsFee(fieldsFee, projectInfo)
               + claim.ClaimAccommodationFee(projectInfo);
        /******************************************************************
         * If you want to add additional fee to a claim's fee,
         * append your value to the expression above.
         * Example:
         *     return claim.BaseFee(operationDate)
         *         + claim.ClaimFieldsFee(fieldsFee)
         *         + claim.ClaimAccommodationFee()
         *         + claim.SomeOtherBigFee();
         *****************************************************************/
    }

    /// <summary>
    /// Returns claim payment status from total fee and money balance
    /// </summary>
    public static ClaimPaymentStatus GetClaimPaymentStatus(int totalFee, int balance)
    {
        if (totalFee < balance)
        {
            return ClaimPaymentStatus.Overpaid;
        }
        else if (totalFee == balance)
        {
            return ClaimPaymentStatus.Paid;
        }
        else if (balance > 0)
        {
            return ClaimPaymentStatus.MoreToPay;
        }
        else
        {
            return ClaimPaymentStatus.NotPaid;
        }
    }

    /// <summary>
    /// Returns total sum of all money flow operations
    /// </summary>
    public static int GetPaymentSum(this Claim claim)
        => claim.FinanceOperations
            .Where(fo => fo.Approved && fo.MoneyFlowOperation)
            .Sum(fo => fo.MoneyAmount);

    /// <summary>
    /// Calculates total fields fee
    /// </summary>
    private static int CalcClaimFieldsFee(this Claim claim, ProjectInfo projectInfo)
    {
        return claim.GetFields(projectInfo).Sum(f => f.GetCurrentFee());
    }

    /// <summary>
    /// Returns actual total claim fields fee
    /// </summary>
    private static int ClaimFieldsFee(this Claim claim, int? fieldsFee, ProjectInfo projectInfo)
    {
        if (fieldsFee == null)
        {
            fieldsFee = claim.FieldsFee ?? claim.CalcClaimFieldsFee(projectInfo);
        }
        // cache
        claim.FieldsFee = fieldsFee;

        return fieldsFee ?? 0;
    }

    /// <summary>
    /// Returns accommodation fee
    /// </summary>
    public static int ClaimAccommodationFee(this Claim claim, ProjectInfo projectInfo)
        => claim.GetAccommodationType(projectInfo)?.Cost ?? 0;

    /// <summary>
    /// Баланс заявки поверх EF-сущности.
    /// </summary>
    /// <remarks>
    /// Легаси-путь: считает по графу EF и ленивым навигациям. Замена — перегрузка поверх доменного
    /// агрегата, <see cref="ClaimBalanceExtensions.CalculateBalance(ClaimInCharacter, DateTime?)"/>
    /// (ADR013); она не обращается к EF вовсе.
    /// </remarks>
    [Obsolete("Используйте ClaimBalanceExtensions.CalculateBalance поверх ClaimInCharacter (ADR013, ADR021)")]
    public static ClaimBalance CalculateClaimBalance(this Claim claim, ProjectInfo projectInfo, DateTime? date = null)
    {
        var paid = claim.ApprovedFinanceOperations.Sum(fo => fo.MoneyAmount);
        var total = claim.ClaimTotalFee(date ?? DateTime.UtcNow, null, projectInfo);
        return new ClaimBalance(paid, total);
    }

    /// <summary>
    /// Returns sum of all approved finance operations
    /// </summary>
    ///
    [Obsolete("CalculateClaimBalance")]
    public static int ClaimBalance(this Claim claim)
        => claim.ApprovedFinanceOperations.Sum(fo => fo.MoneyAmount);

    /// <summary>
    /// Фиксирует за заявкой базовый взнос, если она оплачена полностью
    /// (<see cref="ClaimBalanceExtensions.GetFeeToFix"/>).
    /// </summary>
    /// <param name="claim">Трекаемая EF-сущность заявки, которую мутирует операция.</param>
    /// <param name="snapshot">Доменный снимок той же заявки (ADR021), снятый до операции.</param>
    /// <param name="operationDate">Дата финансовой операции.</param>
    /// <param name="paymentAdded">
    /// На сколько операция меняет сумму подтверждённых платежей: подтверждённый платёж или
    /// возврат, 0 — если деньги не двигались. В снимке этой суммы ещё нет.
    /// </param>
    /// <remarks>
    /// Уплаченное — из снимка плюс то, что добавила операция; взнос за поля и стоимость
    /// проживания — из снимка (ADR022, без навигации группы проживающих). Льгота и
    /// зафиксированный взнос — скаляры трекаемой сущности: их операция может поменять сама.
    /// </remarks>
    /// <exception cref="ArgumentException">Снимок от другой заявки.</exception>
    public static void UpdateClaimFeeIfRequired(
        this Claim claim,
        ClaimInCharacter snapshot,
        DateTime operationDate,
        int paymentAdded)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(snapshot);

        if (claim.GetId() != snapshot.ClaimId)
        {
            throw new ArgumentException(
                $"Snapshot of claim {snapshot.ClaimId} does not match claim {claim.GetId()}", nameof(snapshot));
        }

        var finance = snapshot.Claim.Finance with
        {
            FixedFee = claim.CurrentFee,
            PreferentialFeeUser = claim.PreferentialFeeUser,
            FeePaid = snapshot.Claim.Finance.FeePaid + paymentAdded,
        };
        var changed = new ClaimInCharacter(snapshot.Character, snapshot.Claim with { Finance = finance });

        if (changed.GetFeeToFix(operationDate) is int fee)
        {
            claim.CurrentFee = fee;
        }
    }

    public static IEnumerable<MoneyTransfer> Approved(
        this IEnumerable<MoneyTransfer> transfers)
        => transfers.Where(mt => mt.ResultState == MoneyTransferState.Approved);

    public static IEnumerable<MoneyTransfer> SendedByMaster(
        this IEnumerable<MoneyTransfer> transfers,
        UserIdentification masterId) => transfers.Where(mt => mt.SenderId == masterId.Value);

    public static IEnumerable<MoneyTransfer> ReceivedByMaster(
        this IEnumerable<MoneyTransfer> transfers,
        UserIdentification masterId) => transfers.Where(mt => mt.ReceiverId == masterId.Value);

    public static int SendedByMasterSum(this IReadOnlyCollection<MoneyTransfer> transfers,
        UserIdentification masterId) => transfers.Approved().SendedByMaster(masterId).Sum(mt => -mt.Amount);

    public static int ReceivedByMasterSum(this IReadOnlyCollection<MoneyTransfer> transfers,
        UserIdentification masterId) => transfers.Approved().ReceivedByMaster(masterId).Sum(mt => mt.Amount);
}
