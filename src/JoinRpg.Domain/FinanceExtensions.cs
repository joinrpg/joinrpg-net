using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain;

public static class FinanceExtensions
{
    /// <summary>
    /// Returns total sum of all money flow operations
    /// </summary>
    public static int GetPaymentSum(this Claim claim)
        => claim.FinanceOperations
            .Where(fo => fo.Approved && fo.MoneyFlowOperation)
            .Sum(fo => fo.MoneyAmount);

    /// <summary>
    /// Returns sum of all approved finance operations
    /// </summary>
    [Obsolete("Используйте ClaimFinanceInfo.FeePaid из снимка заявки (ADR013), см. #5467")]
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
