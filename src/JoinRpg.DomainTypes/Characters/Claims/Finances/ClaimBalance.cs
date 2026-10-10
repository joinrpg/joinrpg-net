namespace JoinRpg.DomainTypes.Characters.Claims.Finances;

public record class ClaimBalance(int FeePaid, int TotalFee)
{
    public int FeeDue { get; } = TotalFee - FeePaid;

    /// <summary>Статус оплаты: сколько уплачено относительно начисленного взноса.</summary>
    public ClaimPaymentStatus PaymentStatus
        => FeePaid > TotalFee ? ClaimPaymentStatus.Overpaid
        : FeePaid == TotalFee ? ClaimPaymentStatus.Paid
        : FeePaid > 0 ? ClaimPaymentStatus.MoreToPay
        : ClaimPaymentStatus.NotPaid;

    /// <summary>Взнос оплачен полностью (в том числе с переплатой).</summary>
    public bool IsPaid => PaymentStatus is ClaimPaymentStatus.Paid or ClaimPaymentStatus.Overpaid;
}
