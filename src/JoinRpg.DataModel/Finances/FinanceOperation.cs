using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes.Claims.Finances;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by LINQ
public class FinanceOperation : IProjectEntity, IValidatableObject
{
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }
    public int ClaimId { get; set; }
    public virtual Claim Claim { get; set; }
    public int MoneyAmount { get; set; }
    public int? PaymentTypeId { get; set; }

    public virtual PaymentType? PaymentType { get; set; }

    public int CommentId { get; set; }
    public virtual Comment Comment { get; set; }

    public DateTime Created { get; set; }
    public DateTime Changed { get; set; }

    public DateTime OperationDate { get; set; }

    public FinanceOperationState State { get; set; }

    /// <summary>
    /// Type of this finance operation
    /// </summary>
    public FinanceOperationType OperationType { get; set; }

    /// <summary>
    /// Source or destination claim Id (used if <see cref="OperationType"/>
    /// is <see cref="FinanceOperationType.TransferTo"/>
    /// or <see cref="FinanceOperationType.TransferFrom"/>
    /// </summary>
    public int? LinkedClaimId { get; set; }

    /// <summary>
    /// Source or destination claim (available if <see cref="OperationType"/>
    /// is <see cref="FinanceOperationType.TransferTo"/>
    /// or <see cref="FinanceOperationType.TransferFrom"/>
    /// </summary>
    public Claim? LinkedClaim { get; set; }

    /// <summary>
    /// Id of a finance operation that was refunded.
    /// </summary>
    public int? RefundedOperationId { get; set; }
    public virtual FinanceOperation? RefundedOperation { get; set; }

    public virtual FinanceOperationBankDetails? BankDetails { get; set; }

    /// <summary>
    /// Id of recurrent payment this operation belongs to
    /// </summary>
    public int? RecurrentPaymentId { get; set; }
    public virtual RecurrentPayment? RecurrentPayment { get; set; }

    /// <summary>
    /// Имеет вид YYYY-MM.
    /// Контроль уникальности списания на уровне БД
    /// </summary>
    public string? ReccurrentPaymentInstanceToken { get; set; }


    public static string MakeInstanceToken(DateTime date) => date.ToString("yyyyMM");

    int IOrderableEntity.Id => ProjectId;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Checking payment type
        switch (OperationType)
        {
            case FinanceOperationType.PreferentialFeeRequest:
            case FinanceOperationType.TransferTo:
            case FinanceOperationType.TransferFrom:
                if (PaymentTypeId != null)
                {
                    yield return new ValidationResult($"Operation type {OperationType} must not have payment type specified", new[] { nameof(PaymentTypeId) });
                }
                break;
            case FinanceOperationType.Submit:
            case FinanceOperationType.Online:
            case FinanceOperationType.Refund:
                if (PaymentTypeId == null)
                {
                    yield return new ValidationResult($"Operation type {OperationType} must have payment type specified", new[] { nameof(PaymentTypeId) });
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Checking money value
        switch (OperationType)
        {
            case FinanceOperationType.PreferentialFeeRequest:
                if (MoneyAmount != 0)
                {
                    yield return new ValidationResult($"Operation type {OperationType} must not have money amount", new[] { nameof(MoneyAmount) });
                }
                break;
            case FinanceOperationType.Submit:
            case FinanceOperationType.Online:
            case FinanceOperationType.TransferFrom:
                if (MoneyAmount <= 0)
                {
                    yield return new ValidationResult($"Operation type {OperationType} must have positive money amount", new[] { nameof(MoneyAmount) });
                }
                break;
            case FinanceOperationType.Refund:
            case FinanceOperationType.TransferTo:
                if (MoneyAmount >= 0)
                {
                    yield return new ValidationResult($"Operation type {OperationType} must have negative money amount", new[] { nameof(MoneyAmount) });
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (LinkedClaimId == null && (OperationType == FinanceOperationType.TransferTo || OperationType == FinanceOperationType.TransferFrom))
        {
            yield return new ValidationResult($"Operation of type {OperationType} must be linked with another claim", new[] { nameof(LinkedClaimId) });
        }
    }

    #region helper properties

    /// <summary>
    /// Moderation is required only for operations that require manual
    /// actions from the master
    /// </summary>
    public bool RequireModeration =>
        State == FinanceOperationState.Proposed
        && (OperationType == FinanceOperationType.Submit
            || OperationType == FinanceOperationType.PreferentialFeeRequest);

    /// <summary>
    /// true if operation was approved
    /// </summary>
    public bool Approved => State == FinanceOperationState.Approved;

    /// <summary>
    /// Returns true if operation is money flow operation (where field <see cref="MoneyAmount"/> is not zero)
    /// </summary>
    public bool MoneyFlowOperation => OperationType >= FinanceOperationType.Submit;

    /// <summary>
    /// Returns true if operation is income operation
    /// </summary>
    public bool IncomeOperation => OperationType == FinanceOperationType.Online
        || OperationType == FinanceOperationType.Submit;

    /// <summary>
    /// Returns true if operation is refund operation
    /// </summary>
    public bool RefundOperation => OperationType == FinanceOperationType.Refund;

    #endregion
}
