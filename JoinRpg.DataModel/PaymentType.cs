using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
    /// <summary>
    /// Payment type kinds
    /// </summary>
    public enum PaymentTypeKind
    {
        /// <summary>
        /// Custom, user-defined payment type
        /// </summary>
        Custom = 0,

        /// <summary>
        /// Cash payment to a master
        /// </summary>
        Cash = 1,

        /// <summary>
        /// Online payment type (with bank card directly on site)
        /// </summary>
        Online = 2,
    }

  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by LINQ
  public class PaymentType : IProjectEntity, IValidatableObject, IDeletableSubEntity
  {
      public int PaymentTypeId { get; set; }
      public int ProjectId { get; set; }
      public virtual Project Project { get; set; }

      public string Name { get; set; }

      public int UserId { get; set; }
      public virtual User User { get; set; }

      /// <summary>
      /// Kind of payment type
      /// </summary>
      public PaymentTypeKind Kind { get; set; }

      public bool IsActive { get; set; }

      public bool IsDefault { get; set; }

      public virtual ICollection<FinanceOperation> Operations { get; set; }

      #region interface implementations
      public bool CanBePermanentlyDeleted => !Operations.Any();
      int IOrderableEntity.Id => ProjectId;
      #endregion


      public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
      {
          if (string.IsNullOrWhiteSpace(Name))
          {
              yield return  new ValidationResult("Name is required");
          }
      }

      public PaymentType() {}

      public PaymentType(PaymentTypeKind kind, int projectId, int userId)
      {
          IsActive = true;
          Kind = kind;
          Name = kind.ToString().ToLowerInvariant();
          ProjectId = projectId;
          UserId = userId;
      }
  }
}
