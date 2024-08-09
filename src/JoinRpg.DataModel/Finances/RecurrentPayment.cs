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

    public string? BankRecurrencyToken { get; set; }

    public string? BankParentPayment { get; set; }
    public string? BankAdditional { get; set; }

    public int PaymentAmount { get; set; }

    int IOrderableEntity.Id => RecurrentPaymentId;
}
