using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by LINQ
    public class FinanceOperation : IProjectEntity, IValidatableObject
    {
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }
        public int ClaimId { get; set; }
        public virtual Claim Claim { get; set; }
        public int FeeChange { get; set; }
        public int MoneyAmount { get; set; }
        public int? PaymentTypeId { get; set; }
        [CanBeNull]
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
        public Claim LinkedClaim { get; set; }

        int IOrderableEntity.Id => ProjectId;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Checking payment type
            switch (OperationType)
            {
#pragma warning disable CS0612 // Type or member is obsolete
                case FinanceOperationType.FeeChange:
#pragma warning restore CS0612 // Type or member is obsolete
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
                case FinanceOperationType.FeeChange:
                    if (MoneyAmount != 0)
                    {
                        yield return new ValidationResult($"Operation type {OperationType} must not have money amount", new[] { nameof(MoneyAmount) });
                    }
                    if (FeeChange == 0)
                    {
                        yield return new ValidationResult($"Operation type {OperationType} must have fee change not equal to zero", new[] { nameof(FeeChange) });
                    }
                    break;
                case FinanceOperationType.PreferentialFeeRequest:
                    if (MoneyAmount != 0 || FeeChange != 0)
                    {
                        yield return new ValidationResult($"Operation type {OperationType} must not have money amount or fee change", new[] { nameof(MoneyAmount), nameof(FeeChange) });
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

    public enum FinanceOperationState
    {
        /// <summary>
        /// Operation approved
        /// </summary>
        Approved,

        /// <summary>
        /// Operation is being processed
        /// </summary>
        Proposed,

        /// <summary>
        /// Operation was declined
        /// </summary>
        Declined,

        /// <summary>
        /// Operation is invalid (typically online payment that doesn't have a corresponding object on the bank side)
        /// </summary>
        Invalid,
    }

    /// <summary>
    /// Types of finance operations
    /// </summary>
    public enum FinanceOperationType
    {
        /// <summary>
        /// Manual fee modification
        /// </summary>
        [Obsolete]
        FeeChange = -1, // TODO: Remove or start use

        /// <summary>
        /// Request for preferential fee
        /// </summary>
        PreferentialFeeRequest = -2,

        /// <summary>
        /// Finance operation submits payment using cash or custom payment type
        /// </summary>
        Submit = 0,

        /// <summary>
        /// Online payment
        /// </summary>
        Online,

        /// <summary>
        /// Refund operation
        /// </summary>
        Refund,

        /// <summary>
        /// Money transfer to another claim
        /// </summary>
        TransferTo,

        /// <summary>
        /// Money transfer from another claim
        /// </summary>
        TransferFrom,
    }
}
