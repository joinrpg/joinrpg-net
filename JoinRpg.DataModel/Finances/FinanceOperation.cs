using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by LINQ
    public class FinanceOperation: IProjectEntity, IValidatableObject
    {
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }
        public int ClaimId { get; set; }
        public virtual Claim Claim { get; set; }
        public int FeeChange { get; set; }
        public int MoneyAmount { get; set; }
        public int? PaymentTypeId { get; set; }
        [CanBeNull]
        public virtual PaymentType PaymentType { get; set; }

        public int CommentId { get; set; }
        public virtual Comment Comment { get; set; }

        public DateTime Created { get; set; }
        public DateTime Changed { get; set; }

        public DateTime OperationDate { get; set; }

        public FinanceOperationState State { get; set; }

        public bool MarkMeAsPreferential { get; set; }


        int IOrderableEntity.Id => ProjectId;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MoneyAmount == 0 && PaymentTypeId != null)
            {
                yield return new ValidationResult("Payment type specified for not payment operation");
            }
            if (MoneyAmount != 0 && PaymentTypeId == null)
            {
                yield return new ValidationResult("Payment type not specified for payment operatio");
            }
        }

        #region helper properties

        public bool RequireModeration => State == FinanceOperationState.Proposed;

        public bool Approved => State == FinanceOperationState.Approved;

        #endregion
    }

    public enum FinanceOperationState
    {
        Approved,
        Proposed,
        Declined,
    }
}
