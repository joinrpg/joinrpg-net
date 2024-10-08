using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

public static class PaymentTypeKindExtensions
{
    public static readonly FrozenSet<PaymentTypeKind> Online =
        FrozenSet.ToFrozenSet([PaymentTypeKind.Online, PaymentTypeKind.OnlineSubscription]);

    public static bool IsOnline(this PaymentTypeKind ptk) => Online.Contains(ptk);
}

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
    public PaymentTypeKind TypeKind { get; set; }

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
            yield return new ValidationResult("Name is required");
        }
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public PaymentType() { }

    /// <summary>
    /// Creates payment type of specified kind for specified project and responsible user
    /// </summary>
    public PaymentType(PaymentTypeKind typeKind, int projectId, int userId)
    {
        IsActive = true;
        TypeKind = typeKind;
        Name = typeKind.ToString().ToLowerInvariant();
        ProjectId = projectId;
        UserId = userId;
    }
}
