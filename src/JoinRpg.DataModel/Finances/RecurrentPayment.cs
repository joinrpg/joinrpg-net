using JoinRpg.Helpers;

namespace JoinRpg.DataModel.Finances;

public class RecurrentPayment : IProjectEntity
{
    public int RecurrentPaymentId { get; set; }
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }
    public int ClaimId { get; set; }
    public virtual Claim Claim { get; set; }

    public int PaymentTypeId { get; set; }
    public virtual PaymentType PaymentType { get; set; } = null!;

    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset? CloseDate { get; set; }

    public int PaymentId { get; set; }

    /// <summary>
    /// Токен рекуррентных платежей.
    /// </summary>
    public string? BankRecurrencyToken { get; set; }

    /// <summary>
    /// Идентификатор родительского платежа, отправленный в банк.
    /// В нашем случае это Id финансовой операции, дополненной слева нулями.
    /// </summary>
    public string? BankParentPayment { get; set; }

    /// <summary>
    /// Сколько денег засунули в самом первом платеже.
    /// </summary>
    public int PaymentAmount { get; set; }

    public RecurrentPaymentStatus Status { get; set; } = RecurrentPaymentStatus.Created;

    int IOrderableEntity.Id => RecurrentPaymentId;
}
