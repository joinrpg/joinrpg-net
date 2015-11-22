using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by LINQ
  public class PaymentType : IProjectEntity, IValidatableObject, IDeletableSubEntity
  {
    public int PaymentTypeId { get; set; }
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }

    public string Name { get; set; }

    public int? UserId { get; set; }
    public virtual User User { get; set; }

    public bool IsCash { get; set; }

    public bool IsActive { get; set; }

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
      if (IsCash && (UserId != null || User != null))
      {
        yield return new ValidationResult("Special payment type should not have user");
      }
      if (!IsCash && UserId == null && User == null)
      {
        yield return new ValidationResult("Common payment types should have user");
      }
    }

    public static PaymentType CreateCash()
    {
      return new PaymentType()
      {
        IsCash = true,
        IsActive = true,
        Name = "наличные", //TODO[Localize]: JoinRpg.DataModel should be localization-neutral. 
      };
    }
  }
}
